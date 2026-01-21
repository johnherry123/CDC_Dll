using System.Buffers.Binary;
using CDC_Dll.Core.Abstractions.Protocol;
using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Domain.Protocol;
using CDC_Dll.Core.Infrastructure.Exceptions;

namespace CDC_Dll.Core.Infrastructure.Protocol;

public sealed class FrameCodec : IFrameCodec
{
    private enum State { SeekSof1, SeekSof2, ReadHeader, ReadPayload, ReadCrc }

    private State _state = State.SeekSof1;

    private readonly byte[] _header = new byte[ProtocolConstants.HeaderLength];
    private int _hPos;

    private byte[] _payload = Array.Empty<byte>();
    private int _pPos;

    private readonly byte[] _crc = new byte[2];
    private int _cPos;

    private FrameHeader _curHeader;

    public void Reset()
    {
        _state = State.SeekSof1;
        _hPos = 0;
        _payload = Array.Empty<byte>();
        _pPos = 0;
        _cPos = 0;
    }

    public IEnumerable<Frame> Feed(ReadOnlySpan<byte> data)
    {
       
        List<Frame>? frames = null;

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
                        _state = State.ReadHeader;
                        _hPos = 0;
                    }
                    else if (b == ProtocolConstants.Sof1)
                    {
                        
                        _state = State.SeekSof2;
                    }
                    else
                    {
                        _state = State.SeekSof1;
                    }
                    break;

                case State.ReadHeader:
                    _header[_hPos++] = b;
                    if (_hPos == ProtocolConstants.HeaderLength)
                    {
                        _curHeader = ParseHeader(_header);

                        if (_curHeader.PayloadLength > ProtocolConstants.MaxPayloadLength)
                        {
                          
                            SoftResetAfterError(lastByte: b);
                            throw new ProtocolException(ErrorCode.ProtocolFrameInvalid, "PayloadLength too large.");
                        }

                        _payload = _curHeader.PayloadLength == 0 ? Array.Empty<byte>() : new byte[_curHeader.PayloadLength];
                        _pPos = 0;
                        _cPos = 0;

                        _state = _curHeader.PayloadLength == 0 ? State.ReadCrc : State.ReadPayload;
                    }
                    break;

                case State.ReadPayload:
                    _payload[_pPos++] = b;
                    if (_pPos == _payload.Length)
                    {
                        _state = State.ReadCrc;
                        _cPos = 0;
                    }
                    break;

                case State.ReadCrc:
                    _crc[_cPos++] = b;
                    if (_cPos == 2)
                    {
                        var expected = BinaryPrimitives.ReadUInt16LittleEndian(_crc);

                 
                        Span<byte> check = stackalloc byte[ProtocolConstants.HeaderLength + _payload.Length];
                        _header.CopyTo(check);
                        if (_payload.Length > 0)
                            _payload.AsSpan().CopyTo(check.Slice(ProtocolConstants.HeaderLength));

                        var actual = CRC16.Compute(check);

                        if (actual != expected)
                        {
                            SoftResetAfterError(lastByte: b);
                            throw new ProtocolException(ErrorCode.ProtocolCRCFailed,
                                $"CRC mismatch expected={expected:X4} actual={actual:X4}");
                        }

                        frames ??= new List<Frame>(2);
                        var payloadMem = _payload.Length == 0 ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(_payload);
                        frames.Add(new Frame(_curHeader, payloadMem));

                        // chuẩn bị frame tiếp theo
                        Reset();
                    }
                    break;
            }
        }

        return frames ?? Enumerable.Empty<Frame>();
    }

    public byte[] Encode(Frame frame)
    {
       
        var total = 2 + ProtocolConstants.HeaderLength + frame.Payload.Length + 2;
        var buf = new byte[total];

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

    private void SoftResetAfterError(byte lastByte)
    {
        
        Reset();
        if (lastByte == ProtocolConstants.Sof1)
            _state = State.SeekSof2;
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
