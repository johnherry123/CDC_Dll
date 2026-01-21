namespace CDC_Dll.Core.Domain.Models;

public sealed class DeviceInfo
{
    public string Vendor { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string FirmwareVersion { get; init; } = string.Empty;    
    public string SerialNumber { get; init; } = string.Empty;   
    public string HardwareVersion { get; init; } = string.Empty;    
}