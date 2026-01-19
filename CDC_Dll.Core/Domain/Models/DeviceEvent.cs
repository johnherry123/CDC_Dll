namespace DeviceService.Core.Domain.Models;

public sealed class DeviceEvent
{
    public ushort Code{ get; init; }
    public string Message{ get; init; } = string.Empty;
    public EventSeverity Severity { get; init; } = EventSeverity.Info;
    public DateTime TimestampUTC { get; init; } = DateTime.UtcNow;
}
