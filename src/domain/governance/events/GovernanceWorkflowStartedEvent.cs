namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceWorkflowStartedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid StartedByGuardianId,
    DateTime StartedAt);
