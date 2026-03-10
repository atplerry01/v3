namespace Whycespace.ReliabilityRuntime.Retry;

public sealed class RetryPolicyManager
{
    private readonly int _maxRetries;

    private readonly Dictionary<string, int> _attempts = new();

    public RetryPolicyManager(int maxRetries)
    {
        _maxRetries = maxRetries;
    }

    public bool ShouldRetry(string executionId)
    {
        if (!_attempts.ContainsKey(executionId))
            _attempts[executionId] = 0;

        _attempts[executionId]++;

        return _attempts[executionId] <= _maxRetries;
    }

    public int GetAttempts(string executionId)
    {
        if (!_attempts.TryGetValue(executionId, out var attempts))
            return 0;

        return attempts;
    }
}
