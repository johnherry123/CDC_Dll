namespace CDC_Dll.Core.Domain.Protocol;
public enum ResponeStatus : byte
{
    Ok = 0x00,
    Failed = 0x01,
    Busy = 0x02,
    Invalid = 0x03
}