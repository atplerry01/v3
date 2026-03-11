namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

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

    public GovernanceEmergency TriggerEmergency(string emergencyId, EmergencyType type, string triggeredBy, string reason)
    {
        if (!_guardianStore.Exists(triggeredBy))
            throw new KeyNotFoundException($"Guardian not found: {triggeredBy}");

        var guardian = _guardianStore.GetGuardian(triggeredBy)!;
        if (guardian.Status != GuardianStatus.Active)
            throw new InvalidOperationException("Only active guardians can trigger emergencies.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Emergency reason is required.");

        var emergency = new GovernanceEmergency(
            emergencyId,
            type,
            triggeredBy,
            reason,
            EmergencyStatus.Active,
            DateTime.UtcNow,
            ResolvedAt: null);

        _emergencyStore.Add(emergency);
        return emergency;
    }

    public GovernanceEmergency ResolveEmergency(string emergencyId)
    {
        var emergency = _emergencyStore.Get(emergencyId)
            ?? throw new KeyNotFoundException($"Emergency not found: {emergencyId}");

        if (emergency.Status == EmergencyStatus.Resolved)
            throw new InvalidOperationException("Emergency is already resolved.");

        var updated = emergency with
        {
            Status = EmergencyStatus.Resolved,
            ResolvedAt = DateTime.UtcNow
        };
        _emergencyStore.Update(updated);
        return updated;
    }
}
