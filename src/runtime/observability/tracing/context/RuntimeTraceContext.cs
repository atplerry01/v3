using System.Collections.Concurrent;

using Whycespace.Runtime.Observability.Tracing.Events;

namespace Whycespace.Runtime.Observability.Tracing.Context;

public sealed class RuntimeTraceContext
{
    private readonly ConcurrentDictionary<Guid, CommandTrace> _commandTraces = new();
    private readonly ConcurrentDictionary<Guid, EngineExecutionTrace> _engineTraces = new();
    private readonly ConcurrentDictionary<Guid, EventPublishTrace> _eventTraces = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _correlationIndex = new();

    public CommandTrace StartCommandTrace(Guid commandId, string commandType)
    {
        var trace = CommandTrace.Start(commandId, commandType);
        _commandTraces[trace.TraceId] = trace;
        return trace;
    }

    public void CompleteCommandTrace(Guid traceId)
    {
        if (_commandTraces.TryGetValue(traceId, out var trace))
            _commandTraces[traceId] = trace.Complete();
    }

    public void FailCommandTrace(Guid traceId, string reason)
    {
        if (_commandTraces.TryGetValue(traceId, out var trace))
            _commandTraces[traceId] = trace.Fail(reason);
    }

    public EngineExecutionTrace StartEngineTrace(string engineName, string engineTier)
    {
        var trace = EngineExecutionTrace.Start(engineName, engineTier);
        _engineTraces[trace.TraceId] = trace;
        return trace;
    }

    public void CompleteEngineTrace(Guid traceId, int eventsProduced)
    {
        if (_engineTraces.TryGetValue(traceId, out var trace))
            _engineTraces[traceId] = trace.Complete(eventsProduced);
    }

    public void FailEngineTrace(Guid traceId, string reason)
    {
        if (_engineTraces.TryGetValue(traceId, out var trace))
            _engineTraces[traceId] = trace.Fail(reason);
    }

    public EventPublishTrace RecordEventPublish(
        Guid eventId,
        string eventType,
        string topic,
        Guid? correlationId = null,
        Guid? causationId = null)
    {
        var trace = EventPublishTrace.Create(eventId, eventType, topic, correlationId, causationId);
        _eventTraces[trace.TraceId] = trace;

        if (correlationId.HasValue)
        {
            _correlationIndex.AddOrUpdate(
                correlationId.Value,
                [trace.TraceId],
                (_, list) => { list.Add(trace.TraceId); return list; }
            );
        }

        return trace;
    }

    public CommandTrace? GetCommandTrace(Guid traceId) =>
        _commandTraces.GetValueOrDefault(traceId);

    public EngineExecutionTrace? GetEngineTrace(Guid traceId) =>
        _engineTraces.GetValueOrDefault(traceId);

    public EventPublishTrace? GetEventTrace(Guid traceId) =>
        _eventTraces.GetValueOrDefault(traceId);

    public IReadOnlyList<EventPublishTrace> GetCorrelatedEvents(Guid correlationId)
    {
        if (!_correlationIndex.TryGetValue(correlationId, out var traceIds))
            return [];

        return traceIds
            .Select(id => _eventTraces.GetValueOrDefault(id))
            .Where(t => t is not null)
            .Cast<EventPublishTrace>()
            .ToList();
    }

    public IReadOnlyList<CommandTrace> GetAllCommandTraces() =>
        _commandTraces.Values.ToList();

    public IReadOnlyList<EngineExecutionTrace> GetAllEngineTraces() =>
        _engineTraces.Values.ToList();

    public IReadOnlyList<EventPublishTrace> GetAllEventTraces() =>
        _eventTraces.Values.ToList();
}
