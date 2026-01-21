namespace CDC_Dll.Core.Domain.Protocol;

public enum CommandId : ushort
{
    Ping = 0x0001,
    GetDeviceInfo = 0x0002,
    GetStatus = 0x0003,
    Reset = 0x0004,
    ReadRegisters = 0x0101,
    WriteRegister = 0x0102,
    WriteRegisters = 0x0103,
    Start = 0x0201,
    Stop = 0x0202,
    SetMode = 0x0203,
    SetTelemetryRate = 0x0301,
    SubcribeTelemetry = 0x0302,
    UnsubscribeTelemetry = 0x0303,

}