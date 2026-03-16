namespace Whycespace.Systems.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Models;

public sealed class IdentitySessionStore
{
    private readonly ConcurrentDictionary<Guid, IdentitySession> _sessions = new();

    public void Register(IdentitySession session)
    {
        _sessions[session.SessionId] = session;
    }

    public IdentitySession Get(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new KeyNotFoundException($"Session not found: {sessionId}");

        return session;
    }

    public bool Exists(Guid sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    public void Revoke(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions[sessionId] = session with { Active = false };
        }
    }

    public IReadOnlyCollection<IdentitySession> GetByIdentity(Guid identityId)
    {
        return _sessions.Values
            .Where(x => x.IdentityId == identityId)
            .ToList();
    }
}
