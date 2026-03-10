namespace Whycespace.Reliability.Retry;

public sealed class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;

    public RetryPolicy(int maxRetries = 3, TimeSpan? initialDelay = null)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
    }

    public int MaxRetries => _maxRetries;

    public bool ShouldRetry(int attemptCount)
    {
        return attemptCount < _maxRetries;
    }

    public TimeSpan GetDelay(int attemptCount)
    {
        return _initialDelay * Math.Pow(2, attemptCount);
    }
}
