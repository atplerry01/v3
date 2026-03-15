namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceWorkflowStartedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid StartedByGuardianId,
    DateTime StartedAt);
