using CDC_Dll.Core.Domain.Errors;

namespace CDC_Dll.Core.Infrastructure.Exceptions;
public class DeviceRejectException: Exception
{
    public ErrorCode Code {get; init;}  
    public ushort DeviceErrorCode {get; init;}
    public DeviceRejectException(ErrorCode code, ushort deviceErrorCode, string message)
        : base(message)
    {
        Code = code;
        DeviceErrorCode = deviceErrorCode;
    }
}