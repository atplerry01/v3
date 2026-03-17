namespace Whycespace.Engines.T0U.WhyceGovernance.Engines;

using Whycespace.Engines.T0U.WhyceGovernance.Commands;
using Whycespace.Engines.T0U.WhyceGovernance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceEmergencyEngine
{
    private readonly GovernanceEmergencyStore _emergencyStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceEmergencyEngine(
        GovernanceEmergencyStore emergencyStore,
        GuardianRegistryStore guardianStore)
    {
        _emergencyStore = emergencyStore;
        _guardianStore = guardianStore;
    }

    public GovernanceEmergencyResult Execute(TriggerEmergencyActionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, command.EmergencyType, command.TargetDomain,
                "Emergency reason is required.");

        if (string.IsNullOrWhiteSpace(command.TargetDomain))
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, command.EmergencyType, string.Empty,
                "Target domain is required.");

        var guardian = _guardianStore.GetGuardian(command.TriggeredByGuardianId);
        if (guardian is null)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, command.EmergencyType, command.TargetDomain,
                $"Guardian not found: {command.TriggeredByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, command.EmergencyType, command.TargetDomain,
                $"Only active guardians can trigger emergencies. Guardian status: {guardian.Status}");

        if (_emergencyStore.Exists(command.EmergencyActionId))
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, command.EmergencyType, command.TargetDomain,
                $"Emergency action already exists: {command.EmergencyActionId}");

        var emergency = new GovernanceEmergency(
            command.EmergencyActionId,
            command.EmergencyType,
            command.TargetDomain,
            command.TriggeredByGuardianId,
            command.Reason,
            EmergencyStatus.Active,
            command.Timestamp,
            ResolvedAt: null);

        _emergencyStore.Add(emergency);

        return GovernanceEmergencyResult.Ok(
            command.EmergencyActionId,
            command.EmergencyType,
            EmergencyStatus.Active,
            command.TargetDomain,
            "Emergency action triggered successfully.");
    }

    public GovernanceEmergencyResult Execute(RevokeEmergencyActionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, EmergencyType.SystemPause, string.Empty,
                "Revocation reason is required.");

        var emergency = _emergencyStore.Get(command.EmergencyActionId);
        if (emergency is null)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, EmergencyType.SystemPause, string.Empty,
                $"Emergency not found: {command.EmergencyActionId}");

        if (emergency.Status == EmergencyStatus.Resolved || emergency.Status == EmergencyStatus.Revoked)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, emergency.Type, emergency.TargetDomain,
                $"Emergency cannot be revoked. Current status: {emergency.Status}");

        var guardian = _guardianStore.GetGuardian(command.RevokedByGuardianId);
        if (guardian is null)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, emergency.Type, emergency.TargetDomain,
                $"Guardian not found: {command.RevokedByGuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, emergency.Type, emergency.TargetDomain,
                $"Only active guardians can revoke emergencies. Guardian status: {guardian.Status}");

        var updated = emergency with
        {
            Status = EmergencyStatus.Revoked,
            ResolvedAt = command.Timestamp
        };
        _emergencyStore.Update(updated);

        return GovernanceEmergencyResult.Ok(
            command.EmergencyActionId,
            emergency.Type,
            EmergencyStatus.Revoked,
            emergency.TargetDomain,
            "Emergency action revoked successfully.");
    }

    public GovernanceEmergencyResult Execute(ValidateEmergencyActionCommand command)
    {
        var emergency = _emergencyStore.Get(command.EmergencyActionId);
        if (emergency is null)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, EmergencyType.SystemPause, string.Empty,
                $"Emergency not found: {command.EmergencyActionId}");

        var guardian = _guardianStore.GetGuardian(command.GuardianId);
        if (guardian is null)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, emergency.Type, emergency.TargetDomain,
                $"Guardian not found: {command.GuardianId}");

        if (guardian.Status != GuardianStatus.Active)
            return GovernanceEmergencyResult.Fail(
                command.EmergencyActionId, emergency.Type, emergency.TargetDomain,
                $"Only active guardians can validate emergencies. Guardian status: {guardian.Status}");

        return GovernanceEmergencyResult.Ok(
            command.EmergencyActionId,
            emergency.Type,
            emergency.Status,
            emergency.TargetDomain,
            "Emergency action validated successfully.");
    }

    public GovernanceEmergency? GetEmergency(string emergencyId)
    {
        return _emergencyStore.Get(emergencyId);
    }

    public IReadOnlyList<GovernanceEmergency> ListEmergencies()
    {
        return _emergencyStore.ListAll();
    }
}
