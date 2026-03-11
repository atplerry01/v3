using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceEmergencyEngineTests
{
    private readonly GovernanceEmergencyStore _emergencyStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceEmergencyEngine _engine;

    public GovernanceEmergencyEngineTests()
    {
        _engine = new GovernanceEmergencyEngine(_emergencyStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
        guardianEngine.ActivateGuardian("g-alice");

        guardianEngine.RegisterGuardian("g-inactive", identityId, "Inactive", new List<string>());
    }

    [Fact]
    public void TriggerEmergency_SystemFreeze_Succeeds()
    {
        var emergency = _engine.TriggerEmergency("e-1", EmergencyType.SystemFreeze, "g-alice", "Critical vulnerability detected");

        Assert.Equal("e-1", emergency.EmergencyId);
        Assert.Equal(EmergencyType.SystemFreeze, emergency.Type);
        Assert.Equal("g-alice", emergency.TriggeredBy);
        Assert.Equal(EmergencyStatus.Active, emergency.Status);
        Assert.Null(emergency.ResolvedAt);
    }

    [Fact]
    public void TriggerEmergency_PolicyOverride_Succeeds()
    {
        var emergency = _engine.TriggerEmergency("e-2", EmergencyType.EmergencyPolicyOverride, "g-alice", "Urgent policy change needed");

        Assert.Equal(EmergencyType.EmergencyPolicyOverride, emergency.Type);
    }

    [Fact]
    public void TriggerEmergency_SecurityLockdown_Succeeds()
    {
        var emergency = _engine.TriggerEmergency("e-3", EmergencyType.SecurityLockdown, "g-alice", "Breach detected");

        Assert.Equal(EmergencyType.SecurityLockdown, emergency.Type);
    }

    [Fact]
    public void TriggerEmergency_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.TriggerEmergency("e-bad", EmergencyType.SystemFreeze, "nonexistent", "Reason"));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void TriggerEmergency_InactiveGuardian_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.TriggerEmergency("e-bad", EmergencyType.SystemFreeze, "g-inactive", "Reason"));
        Assert.Contains("Only active guardians", ex.Message);
    }

    [Fact]
    public void TriggerEmergency_EmptyReason_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.TriggerEmergency("e-bad", EmergencyType.SystemFreeze, "g-alice", ""));
        Assert.Contains("reason is required", ex.Message);
    }

    [Fact]
    public void ResolveEmergency_Succeeds()
    {
        _engine.TriggerEmergency("e-res", EmergencyType.SystemFreeze, "g-alice", "Reason");

        var resolved = _engine.ResolveEmergency("e-res");

        Assert.Equal(EmergencyStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public void ResolveEmergency_AlreadyResolved_Throws()
    {
        _engine.TriggerEmergency("e-dup", EmergencyType.SystemFreeze, "g-alice", "Reason");
        _engine.ResolveEmergency("e-dup");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.ResolveEmergency("e-dup"));
        Assert.Contains("already resolved", ex.Message);
    }

    [Fact]
    public void ResolveEmergency_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.ResolveEmergency("nonexistent"));
        Assert.Contains("Emergency not found", ex.Message);
    }
}
