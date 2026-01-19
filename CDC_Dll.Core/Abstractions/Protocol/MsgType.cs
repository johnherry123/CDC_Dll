namespace DeviceService.Core.Abstractions.Protocol;

public enum MsgType : byte
{
    Alive = 0x01,
    Command = 0x02,
    Response = 0x03,
    Telemetry = 0x04,
    Event = 0x05    
}