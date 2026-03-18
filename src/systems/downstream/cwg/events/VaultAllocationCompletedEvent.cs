using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Cwg.Events;

public sealed record VaultAllocationCompletedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    Guid VaultId,
    Guid RecipientIdentityId,
    decimal AllocationPercentage,
    string AllocationType
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static VaultAllocationCompletedEvent Create(Guid allocationId, Guid vaultId, Guid recipientId, decimal percentage, string allocationType) => new(
        Guid.NewGuid(), "VaultAllocationCompletedEvent", allocationId, DateTimeOffset.UtcNow,
        vaultId, recipientId, percentage, allocationType);
}
