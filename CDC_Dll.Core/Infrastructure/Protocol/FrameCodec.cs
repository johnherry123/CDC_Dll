using System.Buffers.Binary;
using DeviceService.Core.Abstractions.Protocol;
using DeviceService.Core.Domain.Errors;
using DeviceService.Core.Domain.Protocol;
using DeviceService.Core.Infrastructure.Exceptions;

namespace CDC_Dll.Core.Infrastructure.Protocol;

public sealed class FrameCodec: IFrameCodec
{
    private enum State{SeekSof1, SeekSof2, Readheader, ReadPayload, ReadCrc}
    private State _state = State.SeekSof1;
    private readonly byte[] _headerBuf = new byte[ProtocolConstants.HeaderLength];
    private int _headerPos;
    private byte[]? _payloadBuf;
    private int _payloadPos;
    private readonly byte[] _crcBuf = new byte[2];
    private int _crcPos;
    private FrameHeader _currentHeader;
    public void Reset()
    {
        _state = State.SeekSof1;
        _headerPos = 0;
        _payloadBuf = null;
        _payloadPos = 0;
        _crcPos = 0;
    }
    public IEnumerable<Frame> Feed(ReadOnlySpan<byte> data)
    {
        var frames = new List<Frame>();
        for(int i =0 ; i<data.Length; i++)
        {
            var b = data[i];
            switch (_state)
            {
                case State.SeekSof1:
                    if(b == ProtocolConstants.Sof1) _state = State.SeekSof2;
                    break;
                case State.SeekSof2:
                    if(b == ProtocolConstants.Sof2)
                    {
                        _state = State.Readheader;
                        _headerPos = 0;
                    }
                    else
                    {
                        _state = State.SeekSof1;
                    }
                    break;
                case State.Readheader:
                    _headerBuf[_headerPos++] = b;
                    if(_headerPos == ProtocolConstants.HeaderLength)
                    {
                        _currentHeader = ParseHeader(_headerBuf);
                        if(_currentHeader.PayloadLength> ProtocolConstants.MaxPayloadLength)
                        {
                            Reset();
                            throw new ProtocolException(ErrorCode.ProtocolFrameInvalid,"PayloadLength too large." );
                        }
                        _payloadBuf =_currentHeader.PayloadLength ==0? Array.Empty<byte>(): new byte[_currentHeader.PayloadLength];
                        _payloadPos = 0;
                        _crcPos =0;
                        _state =(_currentHeader.PayloadLength == 0 )? State.ReadCrc:State.ReadPayload;
                    }
                    break;
                case State.ReadCrc:
                    _crcBuf[_crcPos++] = b;
                    if(_crcPos == 2)
                    {
                        var expected = BinaryPrimitives.ReadUInt16LittleEndian(_crcBuf);
                        Span<byte> check = stackalloc byte[ProtocolConstants.HeaderLength + (_payloadBuf?.Length??0)];
                        _headerBuf.CopyTo(check);
                        if(_payloadBuf is {Length: > 0})
                            _payloadBuf.AsSpan().CopyTo(check.Slice(ProtocolConstants.HeaderLength));
                        var actual = CRC16.Compute(check);
                        if(actual != expected)
                        {
                            Reset();
                            throw new ProtocolException(ErrorCode.ProtocolCRCFailed, $"CRC mismatch. expected={expected:X4},actual = {actual:X4});
                        }
                        var payload = (_payloadBuf is null || _payloadBuf.Length ==0)? ReadOnlyMemory<byte>.Empty: new ReadOnlyMemory<byte>(_payloadBuf);
                        frames.Add(new Frame(_currentHeader, payload));
                        Reset();
                    }
                    break;
                case State.ReadPayload:
                    _payloadBuf![_payloadPos++] =b;
                    if(_payloadPos == _payloadBuf!.Length)
                    {
                        _state = State.ReadCrc;
                        _crcPos = 0;
                    }
                    break;
            }
        }
    }
}