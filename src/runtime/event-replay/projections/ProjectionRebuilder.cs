using Whycespace.EventFabric.Models;
using Whycespace.EventReplay.Engine;

namespace Whycespace.EventReplay.Projections;

public sealed class ProjectionRebuilder
{
    private readonly EventReplayEngine _engine;
    private readonly Dictionary<string, Action> _resetActions = new();
    private readonly Dictionary<string, Func<IReadOnlyList<EventEnvelope>>> _eventSources = new();

    public ProjectionRebuilder(EventReplayEngine engine)
    {
        _engine = engine;
    }

    public void RegisterProjection(
        string projectionName,
        Action reset,
        Func<IReadOnlyList<EventEnvelope>> eventSource)
    {
        _resetActions[projectionName] = reset;
        _eventSources[projectionName] = eventSource;
    }

    public async Task RebuildAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        if (!_resetActions.TryGetValue(projectionName, out var reset))
            throw new InvalidOperationException($"Projection '{projectionName}' not registered.");

        reset();

        var events = _eventSources[projectionName]();
        await _engine.ReplayEventsAsync(events, cancellationToken);
    }

    public IReadOnlyList<string> GetRegisteredProjections() =>
        _resetActions.Keys.ToList();
}
