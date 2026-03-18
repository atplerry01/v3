namespace Whycespace.Engines.T0U.Governance.Delegation.Assignment;

public sealed record GovernanceDelegationResult(
    bool Success,
    string DelegationId,
    string DelegatorGuardianId,
    string DelegateGuardianId,
    string DelegatedRole,
    string AuthorityDomain,
    DelegationAction Action,
    string Message,
    DateTime ExecutedAt);

public enum DelegationAction
{
    Delegated = 0,
    Revoked = 1
}
