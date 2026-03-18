namespace Whycespace.Engines.T0U.Governance.Domain.Deactivation;

public sealed record DeactivateDomainScopeCommand(
    Guid CommandId,
    string AuthorityDomain,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime Timestamp);
