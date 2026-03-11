using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityAuditEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAuditStore _store;
    private readonly IdentityAuditEngine _engine;

    public IdentityAuditEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityAuditStore();
        _engine = new IdentityAuditEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void RecordEvent_ValidIdentity_ReturnsAuditEvent()
    {
        var identityId = RegisterIdentity();

        var auditEvent = _engine.RecordEvent(identityId, "authentication_attempt", "Login from device X");

        Assert.NotEqual(Guid.Empty, auditEvent.EventId);
        Assert.Equal(identityId, auditEvent.IdentityId);
        Assert.Equal("authentication_attempt", auditEvent.EventType);
        Assert.Equal("Login from device X", auditEvent.Description);
    }

    [Fact]
    public void RecordEvent_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _engine.RecordEvent(missingId, "authentication_attempt", "test"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void RecordEvent_EmptyEventType_Throws()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(
            () => _engine.RecordEvent(identityId, "", "test"));
    }

    [Fact]
    public void GetIdentityAudit_ReturnsHistory()
    {
        var identityId = RegisterIdentity();
        _engine.RecordEvent(identityId, "authentication_attempt", "Login");
        _engine.RecordEvent(identityId, "role_assignment", "Assigned admin");

        var history = _engine.GetIdentityAudit(identityId);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void GetAllAuditEvents_ReturnsGlobalHistory()
    {
        var id1 = RegisterIdentity();
        var id2 = RegisterIdentity();
        _engine.RecordEvent(id1, "authentication_attempt", "Login");
        _engine.RecordEvent(id2, "policy_evaluation", "Access check");
        _engine.RecordEvent(id1, "identity_revocation", "Revoked");

        var all = _engine.GetAllAuditEvents();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void MultipleEvents_AllRecorded()
    {
        var identityId = RegisterIdentity();
        _engine.RecordEvent(identityId, "authentication_attempt", "Login");
        _engine.RecordEvent(identityId, "authorization_decision", "Allowed");
        _engine.RecordEvent(identityId, "device_registration", "New device");
        _engine.RecordEvent(identityId, "policy_evaluation", "Trust check");

        var history = _engine.GetIdentityAudit(identityId);

        Assert.Equal(4, history.Count);
    }
}
