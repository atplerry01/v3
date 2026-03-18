using Whycespace.Shared.Primitives.Common;

namespace Whycespace.Shared.Envelopes;

/// <summary>
/// Canonical event envelope — single source of truth for all event transport.
/// </summary>
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
