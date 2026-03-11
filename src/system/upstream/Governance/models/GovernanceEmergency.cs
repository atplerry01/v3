namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceEmergency(
    string EmergencyId,
    EmergencyType Type,
    string TriggeredBy,
    string Reason,
    EmergencyStatus Status,
    DateTime TriggeredAt,
    DateTime? ResolvedAt);

public enum EmergencyType
{
    SystemFreeze,
    EmergencyPolicyOverride,
    SecurityLockdown
}

public enum EmergencyStatus
{
    Active = 0,
    Resolved = 1
}
