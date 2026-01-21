using System.Buffers.Binary;

namespace CDC_Dll.Core.Application.Commands;

public ref struct PayloadWriter
{
    private Span<byte> _buffer;
    private int _pos;
    public PayloadWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _pos = 0;
    }
    public int Written => _pos;
    public int WriteByte(byte v)
    {
        return _buffer[_pos++] = v;
      
    }
    public void WriteUInt16(ushort v)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(_pos, 2), v);
        _pos += 2;
    }
    public void WriteUInt32(uint v)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(_pos, 4), v);
        _pos += 4;
    }
    public void WriteBytes(ReadOnlySpan<byte> v)
    {
        v.CopyTo(_buffer.Slice(_pos, v.Length));
        _pos += v.Length;
    }
}