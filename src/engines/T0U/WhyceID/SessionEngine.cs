namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class SessionEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentitySessionStore _store;

    public SessionEngine(
        IdentityRegistry registry,
        IdentitySessionStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentitySession CreateSession(Guid identityId, Guid deviceId)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        var session = new IdentitySession(
            Guid.NewGuid(),
            identityId,
            deviceId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(8),
            true);

        _store.Register(session);

        return session;
    }

    public bool ValidateSession(Guid sessionId)
    {
        if (!_store.Exists(sessionId))
            return false;

        var session = _store.Get(sessionId);

        if (!session.Active)
            return false;

        if (session.ExpiresAt < DateTime.UtcNow)
            return false;

        return true;
    }

    public void RevokeSession(Guid sessionId)
    {
        _store.Revoke(sessionId);
    }

    public IReadOnlyCollection<IdentitySession> GetIdentitySessions(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }
}
