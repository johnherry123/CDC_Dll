using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Protocol
{
    internal readonly struct ModbusFrameView
    {
        private readonly ReadOnlyMemory<byte> _frame;

        public ModbusFrameView(ReadOnlyMemory<byte> frameMem)
        {
            _frame = frameMem;
        }

        public ReadOnlySpan<byte> Raw => _frame.Span;
        public int Length => _frame.Length;

        public byte Address => Raw.Length > 0 ? Raw[0] : (byte)0;
        public byte Function => Raw.Length > 1 ? Raw[1] : (byte)0;

        public bool IsException => (Function & 0x80) != 0;
        public byte ExceptionCode => Raw.Length >= 5 ? Raw[2] : (byte)0;

        public ReadOnlySpan<byte> Payload
            => Raw.Length >= 4 ? Raw.Slice(2, Raw.Length - 4) : ReadOnlySpan<byte>.Empty;

        public ushort Crc => Raw.Length >= 2 ? (ushort)(Raw[^2] | (Raw[^1] << 8)) : (ushort)0;

        public ushort ReadU16BE(int payloadOffset)
        {
            var p = Payload;
            if (payloadOffset < 0 || payloadOffset + 2 > p.Length) return 0;
            return BinaryPrimitives.ReadUInt16BigEndian(p.Slice(payloadOffset, 2));
        }
    }
}
