namespace Whycespace.Engines.T0U.Governance.ProposalType.Validation;

public sealed record ValidateProposalTypeCommand(
    Guid CommandId,
    string ProposalType,
    string AuthorityDomain,
    Guid RequestedByGuardianId,
    DateTime Timestamp);
