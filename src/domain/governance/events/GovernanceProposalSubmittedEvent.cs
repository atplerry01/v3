namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceProposalSubmittedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid SubmittedByGuardianId,
    DateTimeOffset SubmittedAt);
