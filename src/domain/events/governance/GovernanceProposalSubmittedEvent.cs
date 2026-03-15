namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceProposalSubmittedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid SubmittedByGuardianId,
    DateTimeOffset SubmittedAt);
