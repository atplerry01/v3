using System.Text;
using Whycespace.EventObservability.Failures;
using Whycespace.EventObservability.Lag;
using Whycespace.EventObservability.Metrics;

namespace Whycespace.EventObservability.Dashboard;

public sealed class EventMetricsExporter
{
    private readonly EventMetrics _metrics;
    private readonly ConsumerLagTracker _lagTracker;
    private readonly DeadLetterTracker _deadLetterTracker;

    public EventMetricsExporter(
        EventMetrics metrics,
        ConsumerLagTracker lagTracker,
        DeadLetterTracker deadLetterTracker)
    {
        _metrics = metrics;
        _lagTracker = lagTracker;
        _deadLetterTracker = deadLetterTracker;
    }

    public string ExportPrometheus()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# HELP events_published_total Total number of events published");
        sb.AppendLine("# TYPE events_published_total counter");
        sb.AppendLine($"events_published_total {_metrics.EventsPublishedTotal}");

        sb.AppendLine("# HELP events_consumed_total Total number of events consumed");
        sb.AppendLine("# TYPE events_consumed_total counter");
        sb.AppendLine($"events_consumed_total {_metrics.EventsConsumedTotal}");

        sb.AppendLine("# HELP dead_letter_events_total Total number of dead letter events");
        sb.AppendLine("# TYPE dead_letter_events_total counter");
        sb.AppendLine($"dead_letter_events_total {_metrics.DeadLetterEventsTotal}");

        sb.AppendLine("# HELP consumer_lag Current consumer lag per topic-partition");
        sb.AppendLine("# TYPE consumer_lag gauge");
        foreach (var lag in _lagTracker.GetAllLag())
        {
            sb.AppendLine($"consumer_lag{{topic_partition=\"{lag.Key}\"}} {lag.Value}");
        }

        sb.AppendLine("# HELP topic_throughput Events published per topic");
        sb.AppendLine("# TYPE topic_throughput counter");
        foreach (var throughput in _metrics.GetTopicThroughput())
        {
            sb.AppendLine($"topic_throughput{{topic=\"{throughput.Key}\"}} {throughput.Value}");
        }

        sb.AppendLine("# HELP partition_throughput Events processed per partition");
        sb.AppendLine("# TYPE partition_throughput counter");
        foreach (var throughput in _metrics.GetPartitionThroughput())
        {
            sb.AppendLine($"partition_throughput{{partition=\"{throughput.Key}\"}} {throughput.Value}");
        }

        return sb.ToString();
    }
}
