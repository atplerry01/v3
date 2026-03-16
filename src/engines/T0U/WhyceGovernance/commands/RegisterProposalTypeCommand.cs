namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record RegisterProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
