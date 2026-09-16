namespace AssetTrack.Core.Abstractions;

/// <summary>Outcome of a business operation: either success, or a user-facing reason it failed.</summary>
public class ServiceResult
{
    public bool Success { get; }
    public string? Error { get; }

    private ServiceResult(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public static ServiceResult Ok() => new(true, null);
    public static ServiceResult Fail(string error) => new(false, error);
}

public class ServiceResult<T>
{
    public bool Success { get; }
    public string? Error { get; }
    public T? Value { get; }

    private ServiceResult(bool success, T? value, string? error)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public static ServiceResult<T> Ok(T value) => new(true, value, null);
    public static ServiceResult<T> Fail(string error) => new(false, default, error);
}
