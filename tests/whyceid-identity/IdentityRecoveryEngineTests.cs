using Whycespace.Engines.T0U.WhyceID;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRecoveryEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRecoveryStore _store;
    private readonly IdentityRecoveryEngine _engine;

    public IdentityRecoveryEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityRecoveryStore();
        _engine = new IdentityRecoveryEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void CreateRecovery_ValidIdentity_ReturnsRecovery()
    {
        var identityId = RegisterIdentity();

        var recovery = _engine.CreateRecovery(identityId, "lost_device");

        Assert.NotEqual(Guid.Empty, recovery.RecoveryId);
        Assert.Equal(identityId, recovery.IdentityId);
        Assert.Equal("lost_device", recovery.Reason);
        Assert.Equal("pending", recovery.Status);
        Assert.Null(recovery.CompletedAt);
    }

    [Fact]
    public void CreateRecovery_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _engine.CreateRecovery(missingId, "lost_device"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void CreateRecovery_EmptyReason_Throws()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(
            () => _engine.CreateRecovery(identityId, ""));
    }

    [Fact]
    public void ApproveRecovery_StatusUpdated()
    {
        var identityId = RegisterIdentity();
        var recovery = _engine.CreateRecovery(identityId, "forgot_credentials");

        _engine.ApproveRecovery(recovery.RecoveryId);

        var updated = _store.Get(recovery.RecoveryId);
        Assert.Equal("approved", updated.Status);
    }

    [Fact]
    public void RejectRecovery_StatusUpdated()
    {
        var identityId = RegisterIdentity();
        var recovery = _engine.CreateRecovery(identityId, "security_compromise");

        _engine.RejectRecovery(recovery.RecoveryId);

        var updated = _store.Get(recovery.RecoveryId);
        Assert.Equal("rejected", updated.Status);
    }

    [Fact]
    public void CompleteRecovery_StatusAndTimestampUpdated()
    {
        var identityId = RegisterIdentity();
        var recovery = _engine.CreateRecovery(identityId, "lost_device");

        _engine.CompleteRecovery(recovery.RecoveryId);

        var updated = _store.Get(recovery.RecoveryId);
        Assert.Equal("completed", updated.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public void GetRecoveries_ReturnsByIdentity()
    {
        var identityId = RegisterIdentity();
        _engine.CreateRecovery(identityId, "lost_device");
        _engine.CreateRecovery(identityId, "forgot_credentials");

        var recoveries = _engine.GetRecoveries(identityId);

        Assert.Equal(2, recoveries.Count);
    }
}
