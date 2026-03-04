using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public static class McuRead
    {
        // ===== DI (1x) =====
        public static bool HomeOX => McuState.Current.In1x[1];        
        public static bool HomeOY => McuState.Current.In1x[2];        
        public static bool HomeOZ => McuState.Current.In1x[3];        
        public static bool Emergency => McuState.Current.In1x[4];     

        // ===== Axis (3x) =====
        public static ushort PosX => McuState.Current.PosX;
        public static ushort SpeedX => McuState.Current.SpeedX;
        public static AxisRunState StateX => McuState.Current.StateX;

        public static ushort PosY => McuState.Current.PosY;
        public static ushort SpeedY => McuState.Current.SpeedY;
        public static AxisRunState StateY => McuState.Current.StateY;

        public static ushort PosZ => McuState.Current.PosZ;
        public static ushort SpeedZ => McuState.Current.SpeedZ;
        public static AxisRunState StateZ => McuState.Current.StateZ;
    }
}
