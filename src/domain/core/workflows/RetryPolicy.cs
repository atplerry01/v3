namespace Whycespace.Domain.Core.Workflows;

/// <summary>
/// Retry strategy defining how delays are calculated between retry attempts.
/// </summary>
public enum RetryStrategy
{
    Immediate,
    FixedDelay,
    ExponentialBackoff
}

/// <summary>
/// Domain model representing a retry policy for workflow step failure recovery.
/// Defines maximum retries, delay calculation strategy, and backoff parameters.
/// </summary>
public sealed record RetryPolicy(
    int MaxRetries,
    RetryStrategy RetryStrategy,
    TimeSpan InitialDelay,
    double BackoffMultiplier
)
{
    public const int MaxAllowedRetries = 10;
    public const double MaxBackoffMultiplier = 10.0;
    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Calculates the delay for a given retry attempt based on the strategy.
    /// </summary>
    public TimeSpan CalculateDelay(int retryCount)
    {
        var delay = RetryStrategy switch
        {
            RetryStrategy.Immediate => TimeSpan.Zero,
            RetryStrategy.FixedDelay => InitialDelay,
            RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, retryCount)),
            _ => TimeSpan.Zero
        };

        return delay > MaxDelay ? MaxDelay : delay;
    }
}
