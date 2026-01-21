using CDC_Dll.Core.Domain.Diagnostics;
using CDC_Dll.Core.Domain.Errors;
using CDC_Dll.Core.Infrastructure.Exceptions;

namespace CDC_Dll.Core.Application.Errors;

public static class ErrorMapper
{
    public static ErrorInfo FromException(Exception ex , OperationContext ctx, string userMessageFallback)
    {
        if(ex is TransportException tex)
        {
            return new ErrorInfo
            {
                Code = tex.Code,
                Category = ErrorCategory.Transport,
                Severity = ErrorServerity.Error,
                UserMessage = userMessageFallback,
                TechnicalMessage = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace,
                Context = ctx   
            };
        }
        else if(ex is ProtocolException pex)
        {
            return new ErrorInfo
            {
                Code = pex.Code,
                Category = ErrorCategory.Protocol,
                Severity = ErrorServerity.Error,
                UserMessage = userMessageFallback,
                TechnicalMessage = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace,
                Context = ctx   
            };
        }
        else if(ex is DeviceRejectException dex)
        {
            return new ErrorInfo
            {
                Code = dex.Code,
                Category = ErrorCategory.Device,
                Severity = ErrorServerity.Warning,
                UserMessage = userMessageFallback,
                TechnicalMessage = dex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace,
                DeviceErrorCode = dex.DeviceErrorCode,
                Context = ctx     
            };
        }
        else if(ex is TimeoutException or OperationCanceledException)
        {
            return new ErrorInfo
            {
                Code = ErrorCode.CommandTimeout,
                Category = ErrorCategory.Timeout,
                Severity = ErrorServerity.Warning,
                UserMessage = userMessageFallback,
                TechnicalMessage = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace,
                Context = ctx
            };
        }
        return new ErrorInfo
        {
          Code = ErrorCode.InternalError,
          Category = ErrorCategory.Internal,
          Severity = ErrorServerity.Error,
          UserMessage = userMessageFallback,
          TechnicalMessage = ex.Message,
          ExceptionType = ex.GetType().FullName,
          StackTrace = ex.StackTrace,
          Context = ctx  
        };
    }
}