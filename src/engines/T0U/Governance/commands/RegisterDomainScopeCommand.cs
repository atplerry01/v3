namespace Whycespace.Engines.T0U.Governance.Commands;

public sealed record RegisterDomainScopeCommand(
    Guid CommandId,
    string AuthorityDomain,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
