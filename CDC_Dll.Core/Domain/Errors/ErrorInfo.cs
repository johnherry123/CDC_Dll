using CDC_Dll.Core.Domain.Diagnostics;
namespace CDC_Dll.Core.Domain.Errors;

public sealed class ErrorInfo
{
    public ErrorCode Code { get; init; } = ErrorCode.InternalError;
    public ErrorCategory Category { get; init; } = ErrorCategory.Internal;
    public ErrorServerity Severity { get; init; } = ErrorServerity.Error;
    public string UserMessage { get; init; } = "An error occurred.";
    public string? TechnicalMessage { get; init; }
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
    public ushort? DeviceErrorCode { get; init; }
    public OperationContext Context { get; init; } = new();
    public override string ToString()
    {
        return $"{Code} |{Category}/{Severity}| {Context.Operation} | {UserMessage} ";
    }

    
}