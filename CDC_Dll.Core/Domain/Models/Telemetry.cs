namespace DeviceService.Core.Domain.Models;

public sealed class Telemetry
{
    public ushort Seq{ get; init; }
    public uint TickMS{ get; init;  }
    public double Value1{ get; init; }
    public double Value2{ get; init; }
    public DateTime TimestampUTC { get; init; } = DateTime.UtcNow;  

}