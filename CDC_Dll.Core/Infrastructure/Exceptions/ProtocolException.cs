using CDC_Dll.Core.Domain.Errors;

namespace CDC_Dll.Core.Infrastructure.Exceptions;

public class ProtocolException: Exception
{

  public ErrorCode Code {get; init;}
    public ProtocolException(ErrorCode code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}