using Whycespace.EventObservability.Failures;
using Whycespace.EventObservability.Lag;
using Whycespace.EventObservability.Metrics;

namespace Whycespace.EventObservability.Alerts;

public sealed class EventAlertService
{
    private readonly EventMetrics _metrics;
    private readonly ConsumerLagTracker _lagTracker;
    private readonly DeadLetterTracker _deadLetterTracker;

    private long _lagThreshold = 1000;
    private int _dlqRateThreshold = 10;
    private long _throughputDropThreshold = 100;
    private long _previousThroughput;

    public EventAlertService(
        EventMetrics metrics,
        ConsumerLagTracker lagTracker,
        DeadLetterTracker deadLetterTracker)
    {
        _metrics = metrics;
        _lagTracker = lagTracker;
        _deadLetterTracker = deadLetterTracker;
    }

    public void Configure(long lagThreshold, int dlqRateThreshold, long throughputDropThreshold)
    {
        _lagThreshold = lagThreshold;
        _dlqRateThreshold = dlqRateThreshold;
        _throughputDropThreshold = throughputDropThreshold;
    }

    public IReadOnlyList<EventAlert> Evaluate()
    {
        var alerts = new List<EventAlert>();

        EvaluateConsumerLag(alerts);
        EvaluateDeadLetterRate(alerts);
        EvaluateThroughputDrop(alerts);

        return alerts;
    }

    private void EvaluateConsumerLag(List<EventAlert> alerts)
    {
        var totalLag = _lagTracker.GetTotalLag();

        if (totalLag > _lagThreshold)
        {
            alerts.Add(new EventAlert(
                AlertSeverity.Warning,
                "ConsumerLag",
                $"Consumer lag ({totalLag}) exceeds threshold ({_lagThreshold})",
                DateTime.UtcNow
            ));
        }

        foreach (var lag in _lagTracker.GetAllLag())
        {
            if (lag.Value > _lagThreshold)
            {
                alerts.Add(new EventAlert(
                    AlertSeverity.Critical,
                    "PartitionLag",
                    $"Partition '{lag.Key}' lag ({lag.Value}) exceeds threshold ({_lagThreshold})",
                    DateTime.UtcNow
                ));
            }
        }
    }

    private void EvaluateDeadLetterRate(List<EventAlert> alerts)
    {
        var recentWindow = DateTime.UtcNow.AddMinutes(-5);
        var recentDlq = _deadLetterTracker.GetRecordsSince(recentWindow).Count;

        if (recentDlq > _dlqRateThreshold)
        {
            alerts.Add(new EventAlert(
                AlertSeverity.Critical,
                "DeadLetterRate",
                $"DLQ rate ({recentDlq} in 5min) exceeds threshold ({_dlqRateThreshold})",
                DateTime.UtcNow
            ));
        }
    }

    private void EvaluateThroughputDrop(List<EventAlert> alerts)
    {
        var currentThroughput = _metrics.EventsPublishedTotal;

        if (_previousThroughput > 0 && currentThroughput < _previousThroughput - _throughputDropThreshold)
        {
            alerts.Add(new EventAlert(
                AlertSeverity.Warning,
                "ThroughputDrop",
                $"Event throughput dropped from {_previousThroughput} to {currentThroughput}",
                DateTime.UtcNow
            ));
        }

        _previousThroughput = currentThroughput;
    }
}

public sealed record EventAlert(
    AlertSeverity Severity,
    string AlertType,
    string Message,
    DateTime Timestamp
);

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
