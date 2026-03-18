namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    string CorrelationId,
    DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Ok(T data, string correlationId)
        => new(true, data, null, correlationId, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(string error, string correlationId)
        => new(false, default, error, correlationId, DateTimeOffset.UtcNow);
}
