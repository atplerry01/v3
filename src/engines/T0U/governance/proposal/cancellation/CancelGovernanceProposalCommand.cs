namespace Whycespace.Engines.T0U.Governance.Proposal.Cancellation;

public sealed record CancelGovernanceProposalCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid CancelledByGuardianId,
    string Reason,
    DateTime Timestamp);
