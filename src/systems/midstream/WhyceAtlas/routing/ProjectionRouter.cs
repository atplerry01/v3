namespace Whycespace.Systems.Midstream.WhyceAtlas.Routing;

using Whycespace.Shared.Projections;
using Whycespace.Shared.Envelopes;

public sealed class ProjectionRouter
{
    private readonly Dictionary<string, IProjection> _projectionMap = new();

    public void Register(string eventType, IProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        _projectionMap[eventType] = projection;
    }

    public async Task RouteEventAsync(EventEnvelope envelope)
    {
        if (_projectionMap.TryGetValue(envelope.EventType, out var projection))
        {
            await projection.HandleAsync(envelope);
        }
    }

    public IReadOnlyCollection<string> GetRegisteredEventTypes() => _projectionMap.Keys.ToList();
}
