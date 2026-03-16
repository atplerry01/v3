namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record ValidateProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string AuthorityDomain,
    Guid RequestedByGuardianId,
    DateTime Timestamp);
