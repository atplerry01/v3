using Whycespace.EventFabric.Models;
using Whycespace.Projections.Registry;

namespace Whycespace.Projections.Engine;

public sealed class ProjectionEngine
{
    private readonly IProjectionRegistry _registry;

    public ProjectionEngine(IProjectionRegistry registry)
    {
        _registry = registry;
    }

    public async Task ProcessAsync(EventEnvelope envelope)
    {
        var projections = _registry.Resolve(envelope.EventType);

        foreach (var projection in projections)
        {
            await projection.HandleAsync(envelope);
        }
    }
}
