namespace DaMaiDeparte.Web.Services;

public enum ServiceError
{
    None = 0,
    NotFound,
    Forbidden,
    InvalidState,
    Conflict,
    Validation
}

public class ServiceResult
{
    protected ServiceResult(bool succeeded, ServiceError error, string? message)
    {
        Succeeded = succeeded;
        Error = error;
        Message = message;
    }

    public bool Succeeded { get; }

    public ServiceError Error { get; }

    /// <summary>User-facing (Romanian) message.</summary>
    public string? Message { get; }

    public static ServiceResult Success(string? message = null) => new(true, ServiceError.None, message);

    public static ServiceResult Failure(ServiceError error, string message) => new(false, error, message);
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(bool succeeded, ServiceError error, string? message, T? value)
        : base(succeeded, error, message)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ServiceResult<T> Success(T value, string? message = null) => new(true, ServiceError.None, message, value);

    public static new ServiceResult<T> Failure(ServiceError error, string message) => new(false, error, message, default);
}

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int TotalCount { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
