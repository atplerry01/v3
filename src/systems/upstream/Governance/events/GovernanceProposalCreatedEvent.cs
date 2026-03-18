namespace Whycespace.Systems.Upstream.Governance.Events;

public sealed record GovernanceProposalCreatedEvent(
    Guid EventId,
    string ProposalId,
    string Title,
    string CreatedBy,
    string ProposalType,
    DateTimeOffset Timestamp);
