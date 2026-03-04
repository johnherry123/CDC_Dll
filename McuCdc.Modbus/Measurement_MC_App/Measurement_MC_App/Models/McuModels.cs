using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public enum LogDir { Tx, Rx, Info, Warn, Error }
    public readonly record struct LogItem(DateTime Timestamp, LogDir Dir, string Message, string? Detail = null);
    public sealed class McuStatus
    {
        public DateTime UpdatedAt;

 
        public readonly bool[] Coil0x = new bool[ModbusRanges.Coil0x_Count + 1]; // index 1..32


        public readonly bool[] In1x = new bool[ModbusRanges.In1x_Count + 1];

 
        public ushort PosX, SpeedX;
        public AxisRunState StateX;

        public ushort PosY, SpeedY;
        public AxisRunState StateY;

        public ushort PosZ, SpeedZ;
        public AxisRunState StateZ;

   
        public ushort TargetX, SpeedTarX, MaxX;
        public ushort TargetY, SpeedTarY, MaxY;
        public ushort TargetZ, SpeedTarZ, MaxZ;

        public bool Input(Input1xAddr addr)
        {
            int idx = (int)addr - ModbusRanges.In1x_Start + 1;
            return idx >= 1 && idx <= ModbusRanges.In1x_Count && In1x[idx];
        }
    }
}
