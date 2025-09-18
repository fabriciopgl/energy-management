namespace EnergyManagement.Core.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = [];

    private Result(bool isSuccess, T? data, string message, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
        Errors = errors ?? [];
    }

    public static Result<T> Success(T data, string message = "") =>
        new(true, data, message);

    public static Result<T> Failure(string message, List<string>? errors = null) =>
        new(false, default, message, errors);
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = [];

    private Result(bool isSuccess, string message, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors ?? [];
    }

    public static Result Success( string message = "") =>
        new(true, message);

    public static Result Failure(string message, List<string>? errors = null) =>
        new(false, message, errors);
}
