namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceWorkflowAdvancedEvent(
    Guid EventId,
    Guid ProposalId,
    string PreviousStep,
    string NextStep,
    Guid AdvancedBy,
    DateTime AdvancedAt);
