namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record DeactivateProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime Timestamp);
