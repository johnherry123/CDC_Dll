using DeviceService.Core.Domain.Contracts;

namespace CDC.Dll.Core.Domain.Contracts;
public sealed class RegisRegisterWriteMultipleRequest
{
    public byte UnitId{get;init; } =1;
    public RegisterType Type {get; init;} = RegisterType.HoldingRegister;
    public ushort StartAddress {get; init;}
    public ushort[] Values {get; init;} = Array.Empty<ushort>();
}