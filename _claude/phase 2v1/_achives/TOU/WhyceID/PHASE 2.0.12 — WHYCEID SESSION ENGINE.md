# WHYCESPACE WBSM v3
# PHASE 2.0.12 — WHYCEID SESSION ENGINE

You are implementing **Phase 2.0.12 of the WhyceID System**.

This phase introduces the **Session Engine**, which manages authenticated sessions
for identities across Whycespace systems.

A session represents an authenticated identity interacting with the platform.

Sessions are created after **successful authentication**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Session data must be stored in **IdentitySessionStore**.

The engine must NOT persist state directly.

---

# SESSION CONCEPT

Session properties:

session id
identity id
device id
created timestamp
expiration timestamp
active state

Session lifecycle:

create session
validate session
revoke session
expire session

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

SessionEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentitySessionStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentitySession.cs

---

# SESSION MODEL

Create:

models/IdentitySession.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentitySession(
    Guid SessionId,
    Guid IdentityId,
    Guid DeviceId,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool Active
);

Validation rules:

SessionId must not be empty
IdentityId must not be empty
DeviceId must not be empty

---

# SESSION STORE

Create:

stores/IdentitySessionStore.cs

Purpose:

• store active sessions
• validate sessions
• revoke sessions
• retrieve identity sessions

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

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

---

# SESSION ENGINE

Create:

src/engines/T0U/WhyceID/SessionEngine.cs

Dependencies:

IdentityRegistry
IdentitySessionStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

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
            true
        );

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

---

# SESSION RULES

Sessions expire after:

8 hours

Sessions become invalid when:

revoked
expired
identity revoked (future policy integration)

---

# TESTING

Create tests:

tests/engines/whyceid/

SessionEngineTests.cs

Test scenarios:

session created successfully
missing identity rejected
session validation success
expired session rejected
revoked session rejected
identity session retrieval
multiple sessions supported

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/sessions

Returns:

identity sessions

Add endpoint:

POST /dev/session/revoke

Input:

sessionId

Revokes session.

Only available in DEBUG mode.

---

# SUCCESS CRITERIA

Build must succeed.

Requirements:

0 warnings
0 errors
all tests passing

Engine must remain stateless.

Store must be thread-safe.

---

# NEXT PHASE

After this phase implement:

2.0.13 Consent Engine

Consent Engine will manage:

user data permissions
system access approvals
regulatory compliance (GDPR / audit)