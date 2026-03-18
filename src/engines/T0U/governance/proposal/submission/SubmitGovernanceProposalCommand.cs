namespace Whycespace.Engines.T0U.Governance.Proposal.Submission;

public sealed record SubmitGovernanceProposalCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid SubmittedByGuardianId,
    DateTime Timestamp);
