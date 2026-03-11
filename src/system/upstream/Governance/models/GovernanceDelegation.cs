namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceDelegation(
    string DelegationId,
    string FromGuardian,
    string ToGuardian,
    string RoleScope,
    DateTime StartTime,
    DateTime EndTime,
    DelegationStatus Status);

public enum DelegationStatus
{
    Active = 0,
    Expired = 1,
    Revoked = 2
}
