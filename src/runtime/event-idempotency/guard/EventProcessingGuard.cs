using Whycespace.EventFabric.Models;
using Whycespace.EventIdempotency.Models;
using Whycespace.EventIdempotency.Registry;

namespace Whycespace.EventIdempotency.Guard;

public sealed class EventProcessingGuard
{
    private readonly EventDeduplicationRegistry _registry;

    public EventProcessingGuard(EventDeduplicationRegistry registry)
    {
        _registry = registry;
    }

    public bool ShouldProcess(EventEnvelope envelope)
    {
        if (_registry.HasProcessed(envelope.EventId))
            return false;

        _registry.MarkProcessed(new ProcessedEvent(
            envelope.EventId,
            envelope.EventType,
            envelope.Topic,
            envelope.PartitionKey.Value
        ));

        return true;
    }
}
