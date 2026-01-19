namespace DeviceService.Core.Domain.Errors;

public enum ErrorCategory : byte
{
    Unknown = 0,
    Transport = 1,
    Protocol = 2,
    Timeout = 3,
    Device = 4,
    Configuration = 5,
    Internal = 6

}