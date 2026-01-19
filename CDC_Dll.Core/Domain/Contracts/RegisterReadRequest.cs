namespace DeviceService.Core.Domain.Contracts;

public sealed class RegisterReadRequest
{
    public byte UnitId { get; init; } =1;
    public RegisterType Type { get; init; } = RegisterType.HoldingRegister;
    public ushort StartAddress { get; init; }
    public ushort Count{ get; init;  } =1;
}