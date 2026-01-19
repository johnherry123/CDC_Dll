using DeviceService.Core.Domain.Protocol;

namespace DeviceService.Core.Application.Commands;

public static class CommandPayloadFactory
{
    public static byte[] Build(CommandId id, ReadOnlySpan<byte> data = default)
    {
        var buf = new byte[2+ data.Length];
        var w = new PayloadWriter(buf);
        w.WriteUInt16((ushort)id);
        if(!data.IsEmpty)
        {
            w.WriteBytes(data);
        }
        return buf;
    }
    public static byte[] BuildPing(uint tickMs)
    {
        Span<byte> data = stackalloc byte[4];
        var w = new PayloadWriter(data);
        w.WriteUInt32(tickMs);
        return Build(CommandId.Ping, data);
    }
    public static byte[] BuildSetTelemetryRate(ushort hz)
    {
        Span<byte> data = stackalloc byte[2];
        var w = new PayloadWriter(data);
        w.WriteUInt16(hz);
        return Build(CommandId.SetTelemetryRate, data);
    }
}