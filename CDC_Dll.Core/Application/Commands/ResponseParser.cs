using DeviceService.Core.Domain.Protocol;

namespace DeviceService.Core.Application.Commands;

public sealed class ParsedResponse
{
    public CommandId CommandId { get; init; }
    public ResponeStatus Status { get; init; }  
    public ushort DeviceErrorCode { get; init; }
    public ReadOnlyMemory<byte> data{ get; init; } = ReadOnlyMemory<byte>.Empty;
}
public static class ResponseParser
{
    public static ParsedResponse Parse(ReadOnlyMemory<byte> payload)
    {
        var span = payload.Span;
        if(span.Length < 5)
        {
            throw new ArgumentException("Response payload too short.");
        }
        var r = new PayloadReader(span);
        var cmd =(CommandId) r.ReadUInt16();
        var st = (ResponeStatus) r.ReadByte();
        var devErr = r.ReadUInt16();
        var rest = span.Slice(5);
        return new ParsedResponse
        {
            CommandId = cmd,
            Status = st,
            DeviceErrorCode = devErr,
            data = rest.ToArray()
        };  


   
    }
}