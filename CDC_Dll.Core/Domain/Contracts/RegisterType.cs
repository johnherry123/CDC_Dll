namespace CDC_Dll.Core.Domain.Contracts;

public enum RegisterType: byte
{
    Coil = 0x01,
    DiscreteInput = 0x02,
    HoldingRegister = 0x03,
    InputRegister = 0x04
}