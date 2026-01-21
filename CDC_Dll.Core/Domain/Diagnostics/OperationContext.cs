namespace CDC_Dll.Core.Domain.Diagnostics;

public class OperationContext
{
    public string Operation{get; init;} = string.Empty;
    public string? Detail{get; init;}
    public string? Component{get; init;}    
    public DateTime UtcTime{get; init;} = DateTime.UtcNow;
    //info error details
    public string? MemberName{get; init;}
    public string? FilePath{get; init;}
    public int LineNumber{get; init;}
}