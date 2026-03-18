
using System.Diagnostics;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.EventObservability.Metrics;

namespace Whycespace.EventObservability.Collectors;

public sealed class PublishMetricsCollector
{
    private readonly EventMetrics _metrics;

    public PublishMetricsCollector(EventMetrics metrics)
    {
        _metrics = metrics;
    }

    public void OnEventPublished(string topic, EventEnvelope envelope)
    {
        _metrics.RecordPublished(topic);
    }

    public async Task<T> MeasurePublishAsync<T>(string topic, Func<Task<T>> publishAction)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await publishAction();
        stopwatch.Stop();

        _metrics.RecordPublished(topic);
        _metrics.RecordProcessingLatency(topic, stopwatch.Elapsed.TotalMilliseconds);

        return result;
    }

    public async Task MeasurePublishAsync(string topic, EventEnvelope envelope, Func<Task> publishAction)
    {
        var stopwatch = Stopwatch.StartNew();
        await publishAction();
        stopwatch.Stop();

        _metrics.RecordPublished(topic);
        _metrics.RecordProcessingLatency(topic, stopwatch.Elapsed.TotalMilliseconds);
    }
}
