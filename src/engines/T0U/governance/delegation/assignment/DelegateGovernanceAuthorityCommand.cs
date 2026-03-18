namespace Whycespace.Engines.T0U.Governance.Delegation.Assignment;

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
