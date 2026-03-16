namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceEmergencyResult(
    bool Success,
    string EmergencyActionId,
    EmergencyType EmergencyType,
    EmergencyStatus EmergencyStatus,
    string TargetDomain,
    string Message,
    DateTime ExecutedAt)
{
    public static GovernanceEmergencyResult Ok(
        string emergencyActionId,
        EmergencyType emergencyType,
        EmergencyStatus emergencyStatus,
        string targetDomain,
        string message)
        => new(true, emergencyActionId, emergencyType, emergencyStatus, targetDomain, message, DateTime.UtcNow);

    public static GovernanceEmergencyResult Fail(
        string emergencyActionId,
        EmergencyType emergencyType,
        string targetDomain,
        string message)
        => new(false, emergencyActionId, emergencyType, EmergencyStatus.Triggered, targetDomain, message, DateTime.UtcNow);
}
