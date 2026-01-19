using DeviceService.Core.Domain.Errors;

namespace DeviceService.Core.Infrastructure.Exceptions;

public class ProtocolException: Exception
{

  public ErrorCode Code {get; init;}
    public ProtocolException(ErrorCode code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}