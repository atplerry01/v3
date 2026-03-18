namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceWorkflowCompletedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid CompletedBy,
    DateTime CompletedAt);
