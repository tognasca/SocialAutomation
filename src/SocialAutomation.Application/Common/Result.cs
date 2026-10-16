// src/SocialAutomation.Application/Common/Result.cs

namespace SocialAutomation.Application.Common;

public class Result
{
    public bool Success { get; }
    public string? Error { get; }
    public int? ErrorCode { get; }

    protected Result(bool success, string? error, int? errorCode)
    {
        Success = success;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Ok() => new(true, null, null);
    public static Result Fail(string error, int? errorCode = null) => new(false, error, errorCode);
    public static Result<T> Ok<T>(T data) => new(data, true, null, null);
    public static Result<T> Fail<T>(string error, int? errorCode = null) => new(default, false, error, errorCode);
}

public class Result<T> : Result
{
    public T? Data { get; }

    internal Result(T? data, bool success, string? error, int? errorCode)
        : base(success, error, errorCode)
    {
        Data = data;
    }
}