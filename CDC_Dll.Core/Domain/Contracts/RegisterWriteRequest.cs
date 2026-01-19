namespace DeviceService.Core.Domain.Contracts;

public sealed class RegisterWriteRequest
{
    public byte UnitId { get; init; } = 1;
    public RegisterType Type { get; init; } = RegisterType.HoldingRegister;
    public ushort Address { get; init; }
    public ushort Value { get; init; }  
}