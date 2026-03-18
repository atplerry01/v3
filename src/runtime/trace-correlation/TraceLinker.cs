namespace Whycespace.Runtime.TraceCorrelation;

public sealed class TraceLinker
{
    private readonly CorrelationRegistry _registry;

    public TraceLinker(CorrelationRegistry registry)
    {
        _registry = registry;
    }

    public CommandEventCorrelation Link(
        string commandId,
        string engineId,
        IReadOnlyList<string> emittedEventIds)
    {
        var correlation = new CommandEventCorrelation(
            CommandId: commandId,
            EngineId: engineId,
            EmittedEventIds: emittedEventIds,
            CorrelatedAt: DateTimeOffset.UtcNow);

        _registry.Register(correlation);
        return correlation;
    }

    public IReadOnlyList<string>? GetEventsForCommand(string commandId)
    {
        return _registry.GetByCommandId(commandId)?.EmittedEventIds;
    }

    public string? GetEngineForCommand(string commandId)
    {
        return _registry.GetByCommandId(commandId)?.EngineId;
    }
}
