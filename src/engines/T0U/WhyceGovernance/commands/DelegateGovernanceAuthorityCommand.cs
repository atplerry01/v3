namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

public sealed record DelegateGovernanceAuthorityCommand(
    Guid CommandId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    string DelegatedRole,
    string AuthorityDomain,
    DateTime DelegationStart,
    DateTime DelegationEnd,
    string Reason,
    string RequestedBy,
    DateTime Timestamp);
