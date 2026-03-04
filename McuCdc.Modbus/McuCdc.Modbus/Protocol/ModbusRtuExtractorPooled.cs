using McuCdc.Modbus.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Protocol
{
    internal sealed class ModbusRtuExtractorPooled
    {
        private const int MaxFrameLen = 256;
        private readonly ByteRingBuffer _rb = new(capacity: 4096);

        public void Reset() => _rb.Clear();

        public void Push(ReadOnlySpan<byte> bytes)
        {
            if (!bytes.IsEmpty)
                _rb.Write(bytes);
        }

        public bool TryPop(out PooledBuffer frame)
        {
            frame = null!;

            while (_rb.Count >= 5)
            {
                if (!_rb.TryPeek(0, out byte addr) || !_rb.TryPeek(1, out byte func))
                    return false;

                if (addr > 247)
                {
                    _rb.Skip(1);
                    continue;
                }

                int len;
                if ((func & 0x80) != 0)
                {
                    len = 5;
                }
                else
                {
                    switch (func)
                    {
                        case 0x01:
                        case 0x02:
                        case 0x03:
                        case 0x04:
                            if (_rb.Count < 3) return false;
                            if (!_rb.TryPeek(2, out byte bc)) return false;
                            if (bc > 252) { _rb.Skip(1); continue; }
                            len = 5 + bc;
                            break;

                        case 0x05:
                        case 0x06:
                        case 0x0F:
                        case 0x10:
                            len = 8;
                            break;

                        default:
                            _rb.Skip(1);
                            continue;
                    }
                }

                if (len < 5 || len > MaxFrameLen)
                {
                    _rb.Skip(1);
                    continue;
                }

                if (_rb.Count < len) return false;

                var pb = new PooledBuffer(len);
                _rb.CopyOut(0, len, pb.Span);

                ushort got = (ushort)(pb.Span[^2] | (pb.Span[^1] << 8));
                ushort calc = ModbusCrc16.Compute(pb.Span.Slice(0, len - 2));
                if (got != calc)
                {
                    pb.Dispose();
                    _rb.Skip(1);
                    continue;
                }

                _rb.Skip(len);
                frame = pb;
                return true;
            }

            return false;
        }
    }
}
