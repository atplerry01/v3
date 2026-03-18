
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;

namespace Whycespace.EventFabric.Publisher;

public sealed class PartitionKeyResolver
{
    public string Resolve(EventEnvelope envelope)
    {
        if (!string.IsNullOrEmpty(envelope.AggregateId))
            return envelope.AggregateId;

        if (!envelope.PartitionKey.IsEmpty)
            return envelope.PartitionKey.Value;

        throw new InvalidOperationException(
            $"Event '{envelope.EventType}' has no AggregateId or PartitionKey. " +
            "Deterministic partition routing requires an AggregateId.");
    }
}
