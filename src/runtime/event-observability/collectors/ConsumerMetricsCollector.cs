using System.Diagnostics;
using Whycespace.EventFabric.Models;
using Whycespace.EventObservability.Metrics;

namespace Whycespace.EventObservability.Collectors;

public sealed class ConsumerMetricsCollector
{
    private readonly EventMetrics _metrics;

    public ConsumerMetricsCollector(EventMetrics metrics)
    {
        _metrics = metrics;
    }

    public void OnEventConsumed(EventEnvelope envelope)
    {
        _metrics.RecordConsumed(envelope.Topic, envelope.PartitionKey.Value);
    }

    public async Task MeasureConsumeAsync(EventEnvelope envelope, Func<Task> consumeAction)
    {
        var stopwatch = Stopwatch.StartNew();
        await consumeAction();
        stopwatch.Stop();

        _metrics.RecordConsumed(envelope.Topic, envelope.PartitionKey.Value);
        _metrics.RecordProcessingLatency(envelope.Topic, stopwatch.Elapsed.TotalMilliseconds);
    }
}
