using Whycespace.Contracts.Primitives;

namespace Whycespace.EventFabric.Models;

public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    string Topic,
    object Payload,
    PartitionKey PartitionKey,
    Timestamp Timestamp,
    string? AggregateId = null,
    long SequenceNumber = 0,
    IReadOnlyDictionary<string, string>? Metadata = null
);
