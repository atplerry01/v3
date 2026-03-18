using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Cwg.Events;

public sealed record ContributionReceivedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    Guid ParticipantId,
    Guid VaultId,
    decimal Amount,
    string ContributionType
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static ContributionReceivedEvent Create(Guid contributionId, Guid participantId, Guid vaultId, decimal amount, string contributionType) => new(
        Guid.NewGuid(), "ContributionReceivedEvent", contributionId, DateTimeOffset.UtcNow,
        participantId, vaultId, amount, contributionType);
}
