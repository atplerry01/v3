namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record DeactivateDomainScopeCommand(
    Guid CommandId,
    string AuthorityDomain,
    string Reason,
    Guid DeactivatedByGuardianId,
    DateTime Timestamp);
