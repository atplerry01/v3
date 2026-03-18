using Whycespace.Engines.T0U.WhyceID.Identity.Creation;
using Whycespace.Engines.T0U.WhyceID.Identity.Attributes;
using Whycespace.Engines.T0U.WhyceID.Identity.Graph;
using Whycespace.Engines.T0U.WhyceID.Authentication;
using Whycespace.Engines.T0U.WhyceID.Authorization.Decision;
using Whycespace.Engines.T0U.WhyceID.Consent;
using Whycespace.Engines.T0U.WhyceID.Trust.Device;
using Whycespace.Engines.T0U.WhyceID.Trust.Scoring;
using Whycespace.Engines.T0U.WhyceID.Federation.Provider;
using Whycespace.Engines.T0U.WhyceID.AccessScope.Assignment;
using Whycespace.Engines.T0U.WhyceID.Audit.Reporting;
using Whycespace.Engines.T0U.WhyceID.Recovery.Execution;
using Whycespace.Engines.T0U.WhyceID.Revocation.Execution;
using Whycespace.Engines.T0U.WhyceID.Roles.Assignment;
using Whycespace.Engines.T0U.WhyceID.Permissions.Grant;
using Whycespace.Engines.T0U.WhyceID.Policy.Enforcement;
using Whycespace.Engines.T0U.WhyceID.Verification.Identity;
using Whycespace.Engines.T0U.WhyceID.Service.Registration;
using Whycespace.Engines.T0U.WhyceID.Session.Creation;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class SessionEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentitySessionStore _sessionStore;
    private readonly SessionEngine _engine;

    public SessionEngineTests()
    {
        _registry = new IdentityRegistry();
        _sessionStore = new IdentitySessionStore();
        _engine = new SessionEngine(_registry, _sessionStore);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void CreateSession_ShouldSucceed()
    {
        var identityId = RegisterIdentity();
        var deviceId = Guid.NewGuid();

        var session = _engine.CreateSession(identityId, deviceId);

        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.Equal(identityId, session.IdentityId);
        Assert.Equal(deviceId, session.DeviceId);
        Assert.True(session.Active);
    }

    [Fact]
    public void CreateSession_MissingIdentity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateSession(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void ValidateSession_ShouldReturnTrue_ForActiveSession()
    {
        var identityId = RegisterIdentity();
        var session = _engine.CreateSession(identityId, Guid.NewGuid());

        Assert.True(_engine.ValidateSession(session.SessionId));
    }

    [Fact]
    public void ValidateSession_ExpiredSession_ShouldReturnFalse()
    {
        var identityId = RegisterIdentity();

        // Register an already-expired session directly in the store
        var expired = new IdentitySession(
            Guid.NewGuid(),
            identityId,
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-10),
            DateTime.UtcNow.AddHours(-2),
            true);
        _sessionStore.Register(expired);

        Assert.False(_engine.ValidateSession(expired.SessionId));
    }

    [Fact]
    public void ValidateSession_RevokedSession_ShouldReturnFalse()
    {
        var identityId = RegisterIdentity();
        var session = _engine.CreateSession(identityId, Guid.NewGuid());

        _engine.RevokeSession(session.SessionId);

        Assert.False(_engine.ValidateSession(session.SessionId));
    }

    [Fact]
    public void ValidateSession_NonExistent_ShouldReturnFalse()
    {
        Assert.False(_engine.ValidateSession(Guid.NewGuid()));
    }

    [Fact]
    public void GetIdentitySessions_ShouldReturnAllSessions()
    {
        var identityId = RegisterIdentity();
        _engine.CreateSession(identityId, Guid.NewGuid());
        _engine.CreateSession(identityId, Guid.NewGuid());
        _engine.CreateSession(identityId, Guid.NewGuid());

        var sessions = _engine.GetIdentitySessions(identityId);
        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void GetIdentitySessions_UnknownIdentity_ShouldReturnEmpty()
    {
        var sessions = _engine.GetIdentitySessions(Guid.NewGuid());
        Assert.Empty(sessions);
    }
}
