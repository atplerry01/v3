namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceProposalCancelledEvent(
    Guid EventId,
    Guid ProposalId,
    Guid CancelledByGuardianId,
    string Reason,
    DateTimeOffset CancelledAt);
