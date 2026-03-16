using Whycespace.Contracts.Primitives;

namespace Whycespace.ProjectionRuntime.Projections.Contracts;

public sealed record ProjectionEvent(
    Guid EventId,
    string EventType,
    string AggregateId,
    long SequenceNumber,
    object Payload,
    PartitionKey PartitionKey,
    Timestamp Timestamp
);
