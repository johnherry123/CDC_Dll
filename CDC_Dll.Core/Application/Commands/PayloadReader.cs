using System.Buffers.Binary;

namespace DeviceService.Core.Application.Commands;

public ref struct PayloadReader
{
    private ReadOnlySpan<byte> _buffer;
    private int _pos;
    public PayloadReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _pos = 0;   
    }
    public int Remaining => _buffer.Length - _pos;
    public byte ReadByte() => _buffer[_pos++];
    public ushort ReadUInt16()
    {
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_pos, 2));
        _pos += 2;
        return v;
    }
    public uint ReadUInt32()
    {
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_pos, 4));
        _pos += 4;
        return v;
    }   
    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var v = _buffer.Slice(_pos, length);
        _pos += length;
        return v;
    }

}