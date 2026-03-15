namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceEmergency(
    string EmergencyId,
    EmergencyType Type,
    string TargetDomain,
    string TriggeredBy,
    string Reason,
    EmergencyStatus Status,
    DateTime TriggeredAt,
    DateTime? ResolvedAt);

public enum EmergencyType
{
    SystemPause = 0,
    ClusterFreeze = 1,
    PolicyOverride = 2,
    ExecutionHalt = 3,
    SecurityLockdown = 4,
    EmergencyVoteOverride = 5
}

public enum EmergencyStatus
{
    Triggered = 0,
    Active = 1,
    Resolved = 2,
    Revoked = 3
}
