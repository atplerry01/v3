namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceWorkflowCompletedEvent(
    Guid EventId,
    Guid ProposalId,
    Guid CompletedBy,
    DateTime CompletedAt);
