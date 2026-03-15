namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record RegisterProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
