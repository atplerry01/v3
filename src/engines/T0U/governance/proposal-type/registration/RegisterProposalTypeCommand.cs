namespace Whycespace.Engines.T0U.Governance.ProposalType.Registration;

public sealed record RegisterProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
