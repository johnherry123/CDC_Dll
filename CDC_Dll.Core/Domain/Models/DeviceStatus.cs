using System.Net.NetworkInformation;

namespace DeviceService.Core.Domain.Models;

public sealed class DeviceStatus
{
    public bool isRunning { get; init; }
    public ushort Mode{ get; init;  }
    public ushort ErrorCode { get; init; }  
    public DateTime LastUpdatedUTC { get; init; } = DateTime.UtcNow;

}