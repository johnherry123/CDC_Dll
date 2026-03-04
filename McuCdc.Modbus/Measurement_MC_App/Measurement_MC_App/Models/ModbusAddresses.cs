using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Measurement_MC_App.Models
{
    public enum Coil0xAddr : ushort
    {
        Go_Sensor_Home = 1,  // 00001
        Set_Point = 2,  // 00002 
        Emergency = 3,  // 00003 
        Restart = 4,  // 00004
        STOP = 5,  // 00005 
        Ox_Sub = 9,   // 00009 
        Ox_Plus = 10,  // 00010 
        Oy_Sub = 11,  // 00011 
        Oy_Plus = 12,  // 00012 
        Oz_Sub = 13,  // 00013 
        Oz_Plus = 14,  // 00014 
        Output_1 = 17, // 00017
        Output_2 = 18, // 00018
        Output_3 = 19, // 00019
        Output_4 = 20, // 00020
        Output_5 = 21, // 00021
        Output_6 = 22, // 00022
        Output_7 = 23, // 00023
        Output_8 = 24, // 00024
        Output_9 = 25, // 00025
        Output_10 = 26, // 00026
        Output_11 = 27, // 00027
        Output_12 = 28, // 00028
        Output_13 = 29, // 00029
        Output_14 = 30, // 00030
        Output_15 = 31, // 00031
        Output_16 = 32, // 00032

    }

    public enum CoilBehavior
    {
        Pulse100ms,
        Hold,
        Toggle
    }
    public static class Coil0xRules
    {
        public static CoilBehavior BehaviorOf(Coil0xAddr a) => a switch
        {
            Coil0xAddr.Go_Sensor_Home or
            Coil0xAddr.Set_Point or
            Coil0xAddr.Emergency or
            Coil0xAddr.Restart or
            Coil0xAddr.STOP
                => CoilBehavior.Pulse100ms,

            Coil0xAddr.Ox_Sub or
            Coil0xAddr.Ox_Plus or
            Coil0xAddr.Oy_Sub or
            Coil0xAddr.Oz_Plus or
            Coil0xAddr.Oz_Sub or
            Coil0xAddr.Oz_Plus
                => CoilBehavior.Hold,

            _ => CoilBehavior.Toggle,
        };
    }
    public enum Input1xAddr : ushort
    {
        Input_1 = 1,
        Input_2 = 2,
        Input_3 = 3,
        Input_4 = 4,
        Input_5 = 5,
        Input_6 = 6,
        Input_7 = 7,
        Input_8 = 8,
        Input_9 = 9,
        Input_10 = 10,
        Input_11 = 11,
        Input_12 = 12,
        Input_13 = 13,
        Input_14 = 14,
        Input_15 = 15,
        Input_16 = 16,
        Input_17 = 17,
        Input_18 = 18,
    }
    public enum Input3xAddr : ushort
    {
        PosX = 1,
        SpeedX = 2,
        StateX = 3,
        PosY = 4,
        SpeedY = 5,
        StateY = 6,
        PosZ = 7,
        SpeedZ = 8,
        StateZ = 9,
    }
    public enum Hold4xAddr : ushort
    {
        TargetX = 1,
        SpeedTarX = 2,
        MaxX = 3,
        TargetY = 4,
        SpeedTarY = 5,
        MaxY = 6,
        TargetZ = 7,
        SpeedTarZ = 8,
        MaxZ = 9,
    }
    public enum AxisRunState : ushort
    {
        STOP = 0,
        RUN = 1,
        ERROR = 5
    }
    public static class ModbusRanges
    {
        public const ushort Coil0x_Start = 1;
        public const ushort Coil0x_Count = 32;
        public const ushort In1x_Start = 10001;
        public const ushort In1x_Count = 18;
        public const ushort In3x_Start = 30001;
        public const ushort In3x_Count = 9;
        public const ushort Hold4x_Start = 40001;
        public const ushort Hold4x_Count = 9;
    }
}
