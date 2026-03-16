namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record RegisterDomainScopeCommand(
    Guid CommandId,
    string AuthorityDomain,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
