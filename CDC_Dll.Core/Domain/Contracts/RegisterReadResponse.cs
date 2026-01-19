namespace DeviceService.Core.Domain.Contracts;

public sealed class RegisterReadResponse
{
    public byte UnitId { get; init; } =1;
    public RegisterType Type { get; init; } = RegisterType.HoldingRegister;
    public ushort StartAddress { get; init; }
    public ushort[] Value{ get; init;  } = Array.Empty<ushort>();
    
}