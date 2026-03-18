namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceWorkflowAdvancedEvent(
    Guid EventId,
    Guid ProposalId,
    string PreviousStep,
    string NextStep,
    Guid AdvancedBy,
    DateTime AdvancedAt);
