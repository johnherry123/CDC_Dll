using System.Runtime.CompilerServices;
using CDC_Dll.Core.Domain.Diagnostics;
using CDC_Dll.Core.Domain.Errors;

namespace CDC_Dll.Core.Application.Errors;


public static class ResultGuard
{
    public static OperationContext ctx( string operation, string? detail = null , string? component = null, [CallerMemberName] string? memberName = "", [CallerFilePath] string? filePath ="", [CallerLineNumber] int lineNumber = 0)
    {
        return new OperationContext{
            Operation = operation,
            Detail = detail,
            Component = component,
            MemberName = memberName,
            FilePath = filePath,
            LineNumber = lineNumber
        };  
    }

public static Result Fail(Exception ex, OperationContext ctx, string userMessage) => Result.Fail(ErrorMapper.FromException(ex, ctx, userMessage));
public static Result<T> Fail<T>(Exception ex, OperationContext ctx, string userMessage) => Result<T>.Fail(ErrorMapper.FromException(ex, ctx, userMessage));
}
   
