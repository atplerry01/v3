namespace Whycespace.Engines.T0U.Governance.Domain.Registration;

public sealed record RegisterDomainScopeCommand(
    Guid CommandId,
    string AuthorityDomain,
    string Description,
    Guid RegisteredByGuardianId,
    DateTime Timestamp);
