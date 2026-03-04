using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public sealed class McuStatusSnapshot
    {
        public DateTime UpdatedAt { get; init; }

        public bool[] In1x { get; init; } = new bool[ModbusRanges.In1x_Count + 1];
        public bool[] Coil0x { get; init; } = new bool[ModbusRanges.Coil0x_Count + 1];

        public ushort PosX { get; init; }     // 30001
        public ushort SpeedX { get; init; }   // 30002
        public AxisRunState StateX { get; init; } // 30003

        public ushort PosY { get; init; }     // 30004
        public ushort SpeedY { get; init; }   // 30005
        public AxisRunState StateY { get; init; } // 30006

        public ushort PosZ { get; init; }     // 30007
        public ushort SpeedZ { get; init; }   // 30008
        public AxisRunState StateZ { get; init; } // 30009

        public static McuStatusSnapshot From(McuStatus st)
        {
            var in1 = new bool[st.In1x.Length];
            Array.Copy(st.In1x, in1, st.In1x.Length);

            var c0 = new bool[st.Coil0x.Length];
            Array.Copy(st.Coil0x, c0, st.Coil0x.Length);

            return new McuStatusSnapshot
            {
                UpdatedAt = st.UpdatedAt,
                In1x = in1,
                Coil0x = c0,

                PosX = st.PosX,
                SpeedX = st.SpeedX,
                StateX = st.StateX,
                PosY = st.PosY,
                SpeedY = st.SpeedY,
                StateY = st.StateY,
                PosZ = st.PosZ,
                SpeedZ = st.SpeedZ,
                StateZ = st.StateZ,
            };
        }
    }
}
