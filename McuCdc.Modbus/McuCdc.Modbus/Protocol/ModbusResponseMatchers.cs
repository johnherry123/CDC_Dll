using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Protocol
{
    internal static class ModbusResponseMatchers
    {
        public static IResponseMatcher For(in ModbusRequestSpec req)
            => req.Function switch
            {
                0x01 => new Fc01Or02Matcher(req.Address, 0x01, req.Quantity),
                0x02 => new Fc01Or02Matcher(req.Address, 0x02, req.Quantity),
                0x03 => new Fc03Or04Matcher(req.Address, 0x03, req.Quantity),
                0x04 => new Fc03Or04Matcher(req.Address, 0x04, req.Quantity),
                0x05 => new Fc05Matcher(req.Address, req.StartAddress, req.Value),
                0x06 => new Fc06Matcher(req.Address, req.StartAddress, req.Value),
                0x0F => new Fc0FOr10Matcher(req.Address, 0x0F, req.StartAddress, req.Quantity),
                0x10 => new Fc0FOr10Matcher(req.Address, 0x10, req.StartAddress, req.Quantity),

                _ => new BasicMatcher(req.Address, req.Function)
            };

        private sealed class BasicMatcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly byte _func;
            public BasicMatcher(byte addr, byte func) { _addr = addr; _func = func; }
            public bool IsMatch(ModbusFrameView f)
                => f.Address == _addr && (f.Function == _func || f.Function == (byte)(_func | 0x80));
        }

        private sealed class Fc01Or02Matcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly byte _func;
            private readonly int _expectedByteCount;

            public Fc01Or02Matcher(byte addr, byte func, ushort qty)
            {
                _addr = addr;
                _func = func;
                _expectedByteCount = ((int)qty + 7) / 8;
            }

            public bool IsMatch(ModbusFrameView f)
            {
                if (f.Address != _addr) return false;
                if (f.Function == (byte)(_func | 0x80)) return true; 
                if (f.Function != _func) return false;

                var p = f.Payload;
                if (p.Length < 1) return false;
                return p[0] == _expectedByteCount;
            }
        }

        private sealed class Fc03Or04Matcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly byte _func;
            private readonly int _expectedByteCount;

            public Fc03Or04Matcher(byte addr, byte func, ushort qty)
            {
                _addr = addr;
                _func = func;
                _expectedByteCount = (int)qty * 2;
            }

            public bool IsMatch(ModbusFrameView f)
            {
                if (f.Address != _addr) return false;
                if (f.Function == (byte)(_func | 0x80)) return true; 
                if (f.Function != _func) return false;

                var p = f.Payload;
                if (p.Length < 1) return false;
                return p[0] == _expectedByteCount;
            }
        }

        private sealed class Fc05Matcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly ushort _coil;
            private readonly ushort _valueEcho;

            public Fc05Matcher(byte addr, ushort coil, ushort value01)
            {
                _addr = addr;
                _coil = coil;
                _valueEcho = value01 != 0 ? (ushort)0xFF00 : (ushort)0x0000;
            }

            public bool IsMatch(ModbusFrameView f)
            {
                if (f.Address != _addr) return false;
                if (f.Function == (byte)(0x05 | 0x80)) return true;
                if (f.Function != 0x05) return false;
                ushort coil = f.ReadU16BE(0);
                ushort val = f.ReadU16BE(2);
                return coil == _coil && val == _valueEcho;
            }
        }

        private sealed class Fc06Matcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly ushort _reg;
            private readonly ushort _value;

            public Fc06Matcher(byte addr, ushort reg, ushort value)
            {
                _addr = addr;
                _reg = reg;
                _value = value;
            }

            public bool IsMatch(ModbusFrameView f)
            {
                if (f.Address != _addr) return false;
                if (f.Function == (byte)(0x06 | 0x80)) return true;
                if (f.Function != 0x06) return false;

                ushort reg = f.ReadU16BE(0);
                ushort val = f.ReadU16BE(2);
                return reg == _reg && val == _value;
            }
        }

        private sealed class Fc0FOr10Matcher : IResponseMatcher
        {
            private readonly byte _addr;
            private readonly byte _func;
            private readonly ushort _start;
            private readonly ushort _qty;

            public Fc0FOr10Matcher(byte addr, byte func, ushort start, ushort qty)
            {
                _addr = addr;
                _func = func;
                _start = start;
                _qty = qty;
            }

            public bool IsMatch(ModbusFrameView f)
            {
                if (f.Address != _addr) return false;
                if (f.Function == (byte)(_func | 0x80)) return true;
                if (f.Function != _func) return false;
                ushort start = f.ReadU16BE(0);
                ushort qty = f.ReadU16BE(2);
                return start == _start && qty == _qty;
            }
        }
    }
}
