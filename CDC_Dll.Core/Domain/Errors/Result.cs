namespace CDC_Dll.Core.Domain.Errors;

public readonly struct Result
{
    public bool IsSuccess { get; init; }
    public ErrorInfo? Error { get; init; }

    private Result(bool OK, ErrorInfo? error )
    {
        IsSuccess = OK;
        Error = error;
    }
    public static Result OK()
    {
        return new Result(true, null);
    }
    public static Result Fail(ErrorInfo error)
    {
        return new Result(false, error);
    }

}
public readonly struct Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public ErrorInfo? Error { get; init; }

    private Result(bool OK, T? data, ErrorInfo? error)
    {
        IsSuccess = OK;
        Data = data;
        Error = error;
    }
    public static Result<T> OK(T data)
    {
        return new Result<T>(true, data, null);
    }
    public static Result<T> Fail(ErrorInfo error)
    {
        return new Result<T>(false, default, error);
    }

}