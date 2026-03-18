namespace Whycespace.Runtime.Persistence.Connection;

public sealed record ConnectionOptions
{
    public string PostgresConnectionString { get; init; } = string.Empty;
    public string RedisConnectionString { get; init; } = string.Empty;
    public int CommandTimeoutSeconds { get; init; } = 30;
    public int MaxPoolSize { get; init; } = 100;
    public int MinPoolSize { get; init; } = 5;
    public bool EnableRetry { get; init; } = true;
    public int MaxRetryCount { get; init; } = 3;
}
