using TaskManagement.Domain.Constants;

namespace TaskManagement.Application.Results;

public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? MsgCode { get; set; }
    public int StatusCode { get; set; }

    public static OperationResult<T> Success(T data, int statusCode = API_STATUS_CODES.OK) => new()
    {
        IsSuccess = true,
        Data = data,
        StatusCode = statusCode
    };

    public static OperationResult<T> Failure(string msgCode, int statusCode) => new() 
    { 
        IsSuccess = false, 
        MsgCode = msgCode, 
        StatusCode = statusCode 
    };
}

public class OperationResult
{
    public bool IsSuccess { get; set; }
    public string? MsgCode { get; set; }
    public int StatusCode { get; set; }
    public static OperationResult Success(int statusCode = API_STATUS_CODES.OK) => new() 
    { 
        IsSuccess = true, 
        StatusCode = statusCode 
    };
    public static OperationResult Failure(string msgCode, int statusCode) => new() 
    { 
        IsSuccess = false, 
        MsgCode = msgCode, 
        StatusCode = statusCode 
    };
}
