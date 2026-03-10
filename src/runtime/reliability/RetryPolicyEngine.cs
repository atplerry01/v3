namespace Whycespace.Runtime.Reliability;

public sealed class RetryPolicyEngine
{
    public int MaxRetries { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, Func<T, bool> shouldRetry)
    {
        var delay = InitialDelay;
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var result = await action();
            if (!shouldRetry(result) || attempt == MaxRetries)
                return result;
            await Task.Delay(delay);
            delay *= 2;
        }
        throw new InvalidOperationException("Retry logic error");
    }
}
