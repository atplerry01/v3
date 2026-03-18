namespace Whycespace.Engines.T4A.Access.Gateway;

using System.Collections.Concurrent;

public sealed class RateLimiter
{
    private readonly int _maxRequestsPerWindow;
    private readonly TimeSpan _windowDuration;
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();

    public RateLimiter(int maxRequestsPerWindow = 100, TimeSpan? windowDuration = null)
    {
        _maxRequestsPerWindow = maxRequestsPerWindow;
        _windowDuration = windowDuration ?? TimeSpan.FromMinutes(1);
    }

    public RateLimitResult Check(string clientKey)
    {
        var now = DateTimeOffset.UtcNow;
        var window = _windows.GetOrAdd(clientKey, _ => new SlidingWindow(_windowDuration));

        window.Prune(now);

        if (window.Count >= _maxRequestsPerWindow)
        {
            var retryAfter = window.OldestTimestamp + _windowDuration - now;
            return RateLimitResult.Limited(retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1));
        }

        window.Record(now);
        return RateLimitResult.Allowed(window.Count, _maxRequestsPerWindow);
    }

    private sealed class SlidingWindow
    {
        private readonly TimeSpan _duration;
        private readonly Queue<DateTimeOffset> _timestamps = new();
        private readonly object _lock = new();

        public SlidingWindow(TimeSpan duration) => _duration = duration;

        public int Count { get { lock (_lock) return _timestamps.Count; } }

        public DateTimeOffset OldestTimestamp
        {
            get { lock (_lock) return _timestamps.Count > 0 ? _timestamps.Peek() : DateTimeOffset.UtcNow; }
        }

        public void Record(DateTimeOffset timestamp)
        {
            lock (_lock) _timestamps.Enqueue(timestamp);
        }

        public void Prune(DateTimeOffset now)
        {
            lock (_lock)
            {
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _duration)
                    _timestamps.Dequeue();
            }
        }
    }
}

public sealed record RateLimitResult(
    bool IsAllowed,
    int CurrentCount,
    int MaxCount,
    TimeSpan? RetryAfter)
{
    public static RateLimitResult Allowed(int current, int max) => new(true, current, max, null);
    public static RateLimitResult Limited(TimeSpan retryAfter) => new(false, 0, 0, retryAfter);
}
