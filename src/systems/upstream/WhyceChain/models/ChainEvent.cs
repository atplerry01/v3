namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainEvent(
    string EventId,
    string Domain,
    string EventType,
    string PayloadHash,
    DateTimeOffset Timestamp);
