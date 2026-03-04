using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McuCdc.Modbus.Protocol
{
    internal readonly record struct ModbusRequestSpec(
        byte Address,
        byte Function,
        ushort StartAddress,
        ushort Quantity,
        ushort Value);
}
