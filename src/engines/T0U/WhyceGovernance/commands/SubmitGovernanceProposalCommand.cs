namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record SubmitGovernanceProposalCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid SubmittedByGuardianId,
    DateTime Timestamp);
