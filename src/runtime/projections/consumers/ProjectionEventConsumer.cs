using Whycespace.EventFabric.Models;
using Whycespace.EventIdempotency.Guard;
using Whycespace.Projections.Engine;

namespace Whycespace.Projections.Consumers;

public sealed class ProjectionEventConsumer
{
    private readonly ProjectionEngine _engine;
    private readonly EventProcessingGuard _guard;

    public ProjectionEventConsumer(ProjectionEngine engine, EventProcessingGuard guard)
    {
        _engine = engine;
        _guard = guard;
    }

    public async Task ConsumeAsync(EventEnvelope envelope)
    {
        if (!_guard.ShouldProcess(envelope))
            return;

        await _engine.ProcessAsync(envelope);
    }

    public static IReadOnlyList<string> SubscribedTopics =>
    [
        "whyce.workflow.events",
        "whyce.engine.events",
        "whyce.cluster.events",
        "whyce.economic.events"
    ];
}
