using DeviceService.Core.Domain.Errors;

namespace DeviceService.Core.Infrastructure.Exceptions;

public class TransportException: Exception
{
    public ErrorCode Code {get; init;}
    public TransportException(ErrorCode code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}