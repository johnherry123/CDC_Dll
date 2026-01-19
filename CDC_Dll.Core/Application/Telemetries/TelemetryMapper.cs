using DeviceService.Core.Application.Commands;
using DeviceService.Core.Domain.Models;
using DeviceService.Core.Domain.Protocol;

namespace DeviceService.Core.Application.Telemetries;

public static class TelemetryMapper
{
    public static Telemetry Map(Frame frame)
    {
        var span = frame.Payload.Span;
        if(span.Length<8)
        {
            throw new ArgumentException("Invalid telemetry frame payload length");
        }
        var r = new PayloadReader(span);
        var tick = r.ReadUInt32();
        var v1_u16 = r.ReadUInt16();    
        var v2_u16 = r.ReadUInt16();
        return new Telemetry
        {
             Seq = frame.Header.Seq,
            TickMS = tick,
            Value1 = v1_u16,
            Value2 = v2_u16,
            TimestampUTC = DateTime.UtcNow
        };
    }
}