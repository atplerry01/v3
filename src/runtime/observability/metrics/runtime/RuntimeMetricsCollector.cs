using Whycespace.Runtime.Observability.Tracing.Context;
using Whycespace.Runtime.Observability.Tracing.Events;

namespace Whycespace.Runtime.Observability.Metrics.Runtime;

public sealed class RuntimeMetricsCollector
{
    private readonly RuntimeMetrics _metrics;
    private readonly RuntimeTraceContext _traceContext;

    public RuntimeMetricsCollector(RuntimeMetrics metrics, RuntimeTraceContext traceContext)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _traceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
    }

    public CommandTrace TrackCommandStart(Guid commandId, string commandType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        return _traceContext.StartCommandTrace(commandId, commandType);
    }

    public void TrackCommandComplete(Guid traceId, string commandType)
    {
        _traceContext.CompleteCommandTrace(traceId);
        _metrics.RecordCommandExecuted(commandType, success: true);
    }

    public void TrackCommandFailed(Guid traceId, string commandType, string reason)
    {
        _traceContext.FailCommandTrace(traceId, reason);
        _metrics.RecordCommandExecuted(commandType, success: false);
    }

    public EngineExecutionTrace TrackEngineStart(string engineName, string engineTier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineTier);
        return _traceContext.StartEngineTrace(engineName, engineTier);
    }

    public void TrackEngineComplete(Guid traceId, string engineName, string engineTier, int eventsProduced)
    {
        _traceContext.CompleteEngineTrace(traceId, eventsProduced);

        var trace = _traceContext.GetEngineTrace(traceId);
        var durationMs = trace?.Duration?.TotalMilliseconds ?? 0.0;
        _metrics.RecordEngineExecution(engineName, engineTier, success: true, durationMs);
    }

    public void TrackEngineFailed(Guid traceId, string engineName, string engineTier, string reason)
    {
        _traceContext.FailEngineTrace(traceId, reason);

        var trace = _traceContext.GetEngineTrace(traceId);
        var durationMs = trace?.Duration?.TotalMilliseconds ?? 0.0;
        _metrics.RecordEngineExecution(engineName, engineTier, success: false, durationMs);
    }

    public EventPublishTrace TrackEventPublished(
        Guid eventId,
        string eventType,
        string topic,
        Guid? correlationId = null,
        Guid? causationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        _metrics.RecordEventPublished(eventType, topic);
        return _traceContext.RecordEventPublish(eventId, eventType, topic, correlationId, causationId);
    }

    public RuntimeMetricsSnapshot CreateSnapshot() => RuntimeMetricsSnapshot.From(_metrics);
}
