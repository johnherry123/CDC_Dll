using McuCdc.Modbus.Internal;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Protocol
{
    internal static class ModbusRtuRequestBuilder
    {
        public static PooledBuffer BuildFc01(byte addr, ushort start, ushort qty) => BuildRead(addr, 0x01, start, qty);
        public static PooledBuffer BuildFc02(byte addr, ushort start, ushort qty) => BuildRead(addr, 0x02, start, qty);
        public static PooledBuffer BuildFc03(byte addr, ushort start, ushort qty) => BuildRead(addr, 0x03, start, qty);
        public static PooledBuffer BuildFc04(byte addr, ushort start, ushort qty) => BuildRead(addr, 0x04, start, qty);

        private static PooledBuffer BuildRead(byte addr, byte func, ushort start, ushort qty)
        {
            var pb = new PooledBuffer(8);
            var s = pb.Span;

            s[0] = addr;
            s[1] = func;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(2, 2), start);
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(4, 2), qty);

            ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
            s[6] = (byte)(crc & 0xFF);
            s[7] = (byte)(crc >> 8);
            return pb;
        }

        public static PooledBuffer BuildFc05(byte addr, ushort coil, bool value)
        {
            var pb = new PooledBuffer(8);
            var s = pb.Span;

            s[0] = addr;
            s[1] = 0x05;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(2, 2), coil);
            ushort v = value ? (ushort)0xFF00 : (ushort)0x0000;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(4, 2), v);

            ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
            s[6] = (byte)(crc & 0xFF);
            s[7] = (byte)(crc >> 8);
            return pb;
        }

        public static PooledBuffer BuildFc06(byte addr, ushort reg, ushort value)
        {
            var pb = new PooledBuffer(8);
            var s = pb.Span;

            s[0] = addr;
            s[1] = 0x06;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(2, 2), reg);
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(4, 2), value);

            ushort crc = ModbusCrc16.Compute(s.Slice(0, 6));
            s[6] = (byte)(crc & 0xFF);
            s[7] = (byte)(crc >> 8);
            return pb;
        }

        public static PooledBuffer BuildFc0F(byte addr, ushort start, ReadOnlySpan<bool> values)
        {
            if (values.Length <= 0) throw new ArgumentOutOfRangeException(nameof(values));
            int qty = values.Length;

            int byteCount = (qty + 7) / 8;
            if (byteCount > 246) throw new ArgumentOutOfRangeException(nameof(values), "Too many coils.");

            int len = 9 + byteCount;
            var pb = new PooledBuffer(len);
            var s = pb.Span;

            s[0] = addr;
            s[1] = 0x0F;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(2, 2), start);
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(4, 2), (ushort)qty);
            s[6] = (byte)byteCount;

            var data = s.Slice(7, byteCount);
            data.Clear();
            for (int i = 0; i < qty; i++)
            {
                if (!values[i]) continue;
                data[i >> 3] |= (byte)(1 << (i & 7)); 
            }

            ushort crc = ModbusCrc16.Compute(s.Slice(0, len - 2));
            s[len - 2] = (byte)(crc & 0xFF);
            s[len - 1] = (byte)(crc >> 8);
            return pb;
        }

        public static PooledBuffer BuildFc10(byte addr, ushort start, ReadOnlySpan<ushort> values)
        {
            if (values.Length <= 0) throw new ArgumentOutOfRangeException(nameof(values));
            int qty = values.Length;

            int byteCount = qty * 2;
            if (byteCount > 246) throw new ArgumentOutOfRangeException(nameof(values), "Too many registers.");

            int len = 9 + byteCount;
            var pb = new PooledBuffer(len);
            var s = pb.Span;

            s[0] = addr;
            s[1] = 0x10;
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(2, 2), start);
            BinaryPrimitives.WriteUInt16BigEndian(s.Slice(4, 2), (ushort)qty);
            s[6] = (byte)byteCount;

            var data = s.Slice(7, byteCount);
            int off = 0;
            for (int i = 0; i < qty; i++)
            {
                BinaryPrimitives.WriteUInt16BigEndian(data.Slice(off, 2), values[i]);
                off += 2;
            }

            ushort crc = ModbusCrc16.Compute(s.Slice(0, len - 2));
            s[len - 2] = (byte)(crc & 0xFF);
            s[len - 1] = (byte)(crc >> 8);
            return pb;
        }
    }
}
