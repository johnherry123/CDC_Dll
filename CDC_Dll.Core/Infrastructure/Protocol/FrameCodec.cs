using DeviceService.Core.Abstractions.Protocol;
using DeviceService.Core.Domain.Errors;
using DeviceService.Core.Domain.Protocol;
using DeviceService.Core.Infrastructure.Exceptions;
using System.Buffers.Binary;
using System.Runtime.Intrinsics.Arm;

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

        for (int i = 0; i < data.Length; i++)
        {
            var b = data[i];

            switch (_state)
            {
                case State.SeekSof1:
                    if (b == ProtocolConstants.Sof1) _state = State.SeekSof2;
                    break;

                case State.SeekSof2:
                    if (b == ProtocolConstants.Sof2)
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
                    if (_headerPos == ProtocolConstants.HeaderLength)
                    {
                        _currentHeader = ParseHeader(_headerBuf);

                        if (_currentHeader.PayloadLength > ProtocolConstants.MaxPayloadLength)
                        {
                            Reset();
                            throw new ProtocolException(ErrorCode.ProtocolFrameInvalid, "PayloadLength too large.");
                        }

                        _payloadBuf = _currentHeader.PayloadLength == 0
                            ? Array.Empty<byte>()
                            : new byte[_currentHeader.PayloadLength];

                        _payloadPos = 0;
                        _crcPos = 0;

                        _state = (_currentHeader.PayloadLength == 0) ? State.ReadCrc : State.ReadPayload;
                    }
                    break;

                case State.ReadPayload:
                    _payloadBuf![_payloadPos++] = b;
                    if (_payloadPos == _payloadBuf!.Length)
                    {
                        _state = State.ReadCrc;
                        _crcPos = 0;
                    }
                    break;

                case State.ReadCrc:
                    _crcBuf[_crcPos++] = b;
                    if (_crcPos == 2)
                    {
                
                        var expected = BinaryPrimitives.ReadUInt16LittleEndian(_crcBuf);

                        Span<byte> check = stackalloc byte[ProtocolConstants.HeaderLength + (_payloadBuf?.Length ?? 0)];
                        _headerBuf.CopyTo(check);
                        if (_payloadBuf is { Length: > 0 })
                            _payloadBuf.AsSpan().CopyTo(check.Slice(ProtocolConstants.HeaderLength));

                        var actual = CRC16.Compute(check);

                        if (actual != expected)
                        {
                            Reset();
                            throw new ProtocolException(ErrorCode.ProtocolCRCFailed,
                                $"CRC mismatch. expected={expected:X4}, actual={actual:X4}");
                        }

                        var payload = (_payloadBuf is null || _payloadBuf.Length == 0)
                            ? ReadOnlyMemory<byte>.Empty
                            : new ReadOnlyMemory<byte>(_payloadBuf);

                        frames.Add(new Frame(_currentHeader, payload));

                        Reset();
                    }
                    break;
            }
        }

        return frames;
    }

    public byte[] Encode(Frame frame)
    { 
        var len = 2 + ProtocolConstants.HeaderLength + frame.Payload.Length + 2;
        var buf = new byte[len];
        int p = 0;

        buf[p++] = ProtocolConstants.Sof1;
        buf[p++] = ProtocolConstants.Sof2;

        WriteHeader(frame.Header, buf.AsSpan(p, ProtocolConstants.HeaderLength));
        p += ProtocolConstants.HeaderLength;

        if (frame.Payload.Length > 0)
        {
            frame.Payload.Span.CopyTo(buf.AsSpan(p, frame.Payload.Length));
            p += frame.Payload.Length;
        }

       
        var crcSpan = buf.AsSpan(2, ProtocolConstants.HeaderLength + frame.Payload.Length);
        var crc = CRC16.Compute(crcSpan);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(p, 2), crc);

        return buf;
    }

    private static FrameHeader ParseHeader(ReadOnlySpan<byte> header)
    {
      
        var ver = header[0];
        var type = (MsgType)header[1];
        var msgId = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2, 2));
        var seq = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(4, 2));
        var payloadLen = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(6, 2));

        return new FrameHeader(ver, type, msgId, seq, payloadLen);
    }

    private static void WriteHeader(FrameHeader h, Span<byte> dst)
    {
        dst[0] = h.Version;
        dst[1] = (byte)h.Type;
        BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(2, 2), h.MsgId);
        BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(4, 2), h.Seq);
        BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(6, 2), h.PayloadLength);
    }
}