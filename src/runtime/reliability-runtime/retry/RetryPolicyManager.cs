namespace Whycespace.ReliabilityRuntime.Retry;

/// <summary>
/// Thread Safety Notice
/// --------------------
/// This component is designed for single-threaded runtime access.
///
/// In the Whycespace runtime architecture, execution is serialized
/// through partition workers and workflow dispatchers.
///
/// Because of this guarantee, concurrent collections are not required
/// and standard Dictionary/List structures are used for efficiency.
///
/// If this component is used outside the partition runtime context,
/// external synchronization must be applied.
/// </summary>
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
