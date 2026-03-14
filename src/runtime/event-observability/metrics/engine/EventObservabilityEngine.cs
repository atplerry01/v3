using Whycespace.EventObservability.Metrics.Collector;
using Whycespace.EventObservability.Metrics.Snapshot;

namespace Whycespace.EventObservability.Metrics.Engine;

public sealed class EventObservabilityEngine
{
    private readonly EventMetricsCollector _collector;

    public EventObservabilityEngine(EventMetricsCollector collector)
    {
        _collector = collector;
    }

    public RuntimeMetricsSnapshot GetSnapshot()
    {
        return _collector.GetSnapshot();
    }
}
