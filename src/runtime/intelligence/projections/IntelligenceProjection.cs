namespace Whycespace.IntelligenceRuntime.Projections;

using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;

public sealed class IntelligenceProjection : IProjection
{
    private readonly IntelligenceProjectionRouter _router;

    public IntelligenceProjection(IntelligenceProjectionRouter router)
    {
        _router = router;
    }

    public string Name => "IntelligenceProjection";

    public IReadOnlyCollection<string> EventTypes => _router.GetBoundEventTypes().ToList();

    public async Task HandleAsync(EventEnvelope envelope)
    {
        await _router.ProcessEventAsync(envelope);
    }
}
