namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceProposalCancelledEvent(
    Guid EventId,
    Guid ProposalId,
    Guid CancelledByGuardianId,
    string Reason,
    DateTimeOffset CancelledAt);
