namespace Whycespace.Runtime.Reliability;

public sealed class TimeoutManager
{
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> action, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;
        using var cts = new CancellationTokenSource(effectiveTimeout);
        var task = action();
        var completed = await Task.WhenAny(task, Task.Delay(effectiveTimeout, cts.Token));
        if (completed == task)
            return await task;
        throw new TimeoutException($"Operation timed out after {effectiveTimeout}");
    }
}
