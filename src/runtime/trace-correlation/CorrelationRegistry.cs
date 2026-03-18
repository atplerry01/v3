namespace Whycespace.Runtime.TraceCorrelation;

using System.Collections.Concurrent;

public sealed class CorrelationRegistry
{
    private readonly ConcurrentDictionary<string, CommandEventCorrelation> _correlations = new();

    public void Register(CommandEventCorrelation correlation)
    {
        _correlations[correlation.CommandId] = correlation;
    }

    public CommandEventCorrelation? GetByCommandId(string commandId)
    {
        _correlations.TryGetValue(commandId, out var correlation);
        return correlation;
    }

    public IReadOnlyList<CommandEventCorrelation> GetByEngineId(string engineId)
    {
        return _correlations.Values
            .Where(c => c.EngineId == engineId)
            .OrderByDescending(c => c.CorrelatedAt)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyCollection<CommandEventCorrelation> GetAll()
    {
        return _correlations.Values.ToList().AsReadOnly();
    }

    public int Count => _correlations.Count;
}
