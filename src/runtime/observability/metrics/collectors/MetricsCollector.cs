using System.Collections.Concurrent;

namespace Whycespace.Runtime.Observability.Metrics.Collectors;

public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _metrics = new();

    public void Increment(string metric)
    {
        _metrics.AddOrUpdate(metric, 1, (_, v) => v + 1);
    }

    public long Get(string metric)
    {
        return _metrics.TryGetValue(metric, out var value)
            ? value
            : 0;
    }

    public IReadOnlyDictionary<string, long> GetAll()
    {
        return _metrics;
    }

    public void Reset()
    {
        _metrics.Clear();
    }
}
