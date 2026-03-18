namespace Whycespace.Engines.T0U.Governance.ProposalType.Deactivation;

public sealed record DeactivateProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime Timestamp);
