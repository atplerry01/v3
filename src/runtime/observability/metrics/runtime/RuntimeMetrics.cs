using System.Collections.Concurrent;

namespace Whycespace.Runtime.Observability.Metrics.Runtime;

public sealed class RuntimeMetrics
{
    private long _commandsExecutedTotal;
    private long _commandsSucceededTotal;
    private long _commandsFailedTotal;
    private long _engineExecutionsTotal;
    private long _engineExecutionsSucceededTotal;
    private long _engineExecutionsFailedTotal;
    private long _eventsPublishedTotal;
    private readonly ConcurrentDictionary<string, long> _commandCountsByType = new();
    private readonly ConcurrentDictionary<string, long> _engineCountsByName = new();
    private readonly ConcurrentDictionary<string, long> _engineCountsByTier = new();
    private readonly ConcurrentDictionary<string, long> _eventCountsByType = new();
    private readonly ConcurrentDictionary<string, long> _eventCountsByTopic = new();
    private readonly ConcurrentDictionary<string, double> _engineLatenciesByName = new();

    public long CommandsExecutedTotal => Interlocked.Read(ref _commandsExecutedTotal);
    public long CommandsSucceededTotal => Interlocked.Read(ref _commandsSucceededTotal);
    public long CommandsFailedTotal => Interlocked.Read(ref _commandsFailedTotal);
    public long EngineExecutionsTotal => Interlocked.Read(ref _engineExecutionsTotal);
    public long EngineExecutionsSucceededTotal => Interlocked.Read(ref _engineExecutionsSucceededTotal);
    public long EngineExecutionsFailedTotal => Interlocked.Read(ref _engineExecutionsFailedTotal);
    public long EventsPublishedTotal => Interlocked.Read(ref _eventsPublishedTotal);

    public void RecordCommandExecuted(string commandType, bool success)
    {
        Interlocked.Increment(ref _commandsExecutedTotal);
        _commandCountsByType.AddOrUpdate(commandType, 1, (_, count) => count + 1);

        if (success)
            Interlocked.Increment(ref _commandsSucceededTotal);
        else
            Interlocked.Increment(ref _commandsFailedTotal);
    }

    public void RecordEngineExecution(string engineName, string engineTier, bool success, double durationMs)
    {
        Interlocked.Increment(ref _engineExecutionsTotal);
        _engineCountsByName.AddOrUpdate(engineName, 1, (_, count) => count + 1);
        _engineCountsByTier.AddOrUpdate(engineTier, 1, (_, count) => count + 1);
        _engineLatenciesByName[engineName] = durationMs;

        if (success)
            Interlocked.Increment(ref _engineExecutionsSucceededTotal);
        else
            Interlocked.Increment(ref _engineExecutionsFailedTotal);
    }

    public void RecordEventPublished(string eventType, string topic)
    {
        Interlocked.Increment(ref _eventsPublishedTotal);
        _eventCountsByType.AddOrUpdate(eventType, 1, (_, count) => count + 1);
        _eventCountsByTopic.AddOrUpdate(topic, 1, (_, count) => count + 1);
    }

    public long GetCommandCount(string commandType) =>
        _commandCountsByType.GetValueOrDefault(commandType, 0);

    public long GetEngineExecutionCount(string engineName) =>
        _engineCountsByName.GetValueOrDefault(engineName, 0);

    public double GetEngineLatency(string engineName) =>
        _engineLatenciesByName.GetValueOrDefault(engineName, 0.0);

    public long GetEventCount(string eventType) =>
        _eventCountsByType.GetValueOrDefault(eventType, 0);

    public long GetTopicEventCount(string topic) =>
        _eventCountsByTopic.GetValueOrDefault(topic, 0);

    public IReadOnlyDictionary<string, long> GetCommandCountsByType() => _commandCountsByType;
    public IReadOnlyDictionary<string, long> GetEngineCountsByName() => _engineCountsByName;
    public IReadOnlyDictionary<string, long> GetEngineCountsByTier() => _engineCountsByTier;
    public IReadOnlyDictionary<string, long> GetEventCountsByType() => _eventCountsByType;
    public IReadOnlyDictionary<string, long> GetEventCountsByTopic() => _eventCountsByTopic;
    public IReadOnlyDictionary<string, double> GetEngineLatenciesByName() => _engineLatenciesByName;
}
