# WHYCESPACE WBSM v3
# PHASE 2.0.18 — WHYCEID IDENTITY REVOCATION ENGINE

You are implementing **Phase 2.0.18 of the WhyceID System**.

This phase introduces the **Identity Revocation Engine**, which allows
the system to revoke identities due to security incidents, governance
decisions, or policy enforcement.

Revocation disables identity activity across the entire platform.

Examples:

security breach
policy violation
governance suspension
identity compromise
legal enforcement action

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE  
stateless logic

SYSTEM  
stateful storage

Revocation records must be stored in **IdentityRevocationStore**.

The engine must NOT persist state directly.

---

# REVOCATION CONCEPT

Revocation records track identity suspension or revocation.

Revocation properties:

revocation id  
identity id  
reason  
created timestamp  
active flag  

Revocation lifecycle:

create revocation  
check revocation status  
revoke identity  
restore identity (future extension)

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityRevocationEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityRevocationStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityRevocation.cs

---

# REVOCATION MODEL

Create:

models/IdentityRevocation.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityRevocation(
    Guid RevocationId,
    Guid IdentityId,
    string Reason,
    DateTime CreatedAt,
    bool Active
);

Validation rules:

RevocationId must not be empty  
IdentityId must not be empty  
Reason must not be empty  

Example reasons:

security_breach  
policy_violation  
governance_action  
legal_enforcement

---

# REVOCATION STORE

Create:

stores/IdentityRevocationStore.cs

Purpose:

store revocation records  
retrieve revocation records  
check if identity is revoked  

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityRevocationStore
{
    private readonly ConcurrentDictionary<Guid, IdentityRevocation> _revocations = new();

    public void Register(IdentityRevocation revocation)
    {
        _revocations[revocation.RevocationId] = revocation;
    }

    public bool IsRevoked(Guid identityId)
    {
        return _revocations.Values.Any(r =>
            r.IdentityId == identityId &&
            r.Active);
    }

    public IReadOnlyCollection<IdentityRevocation> GetByIdentity(Guid identityId)
    {
        return _revocations.Values
            .Where(r => r.IdentityId == identityId)
            .ToList();
    }

    public IReadOnlyCollection<IdentityRevocation> GetAll()
    {
        return _revocations.Values.ToList();
    }
}

---

# IDENTITY REVOCATION ENGINE

Create:

src/engines/T0U/WhyceID/IdentityRevocationEngine.cs

Dependencies:

IdentityRegistry  
IdentityRevocationStore  

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityRevocationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRevocationStore _store;

    public IdentityRevocationEngine(
        IdentityRegistry registry,
        IdentityRevocationStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityRevocation RevokeIdentity(Guid identityId, string reason)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Revocation reason cannot be empty.");

        var revocation = new IdentityRevocation(
            Guid.NewGuid(),
            identityId,
            reason,
            DateTime.UtcNow,
            true
        );

        _store.Register(revocation);

        return revocation;
    }

    public bool IsIdentityRevoked(Guid identityId)
    {
        return _store.IsRevoked(identityId);
    }

    public IReadOnlyCollection<IdentityRevocation> GetRevocations(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityRevocation> GetAllRevocations()
    {
        return _store.GetAll();
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityRevocationEngineTests.cs

Test scenarios:

revoke identity successfully  
missing identity rejected  
revocation status detection  
multiple revocations supported  
retrieve revocation records  

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/revocations

Returns revocation records.

Add endpoint:

POST /dev/identity/revoke

Input:

identityId  
reason  

Revokes identity.

Add endpoint:

GET /dev/revocations

Returns all revocation records.

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

2.0.19 Identity Policy Enforcement Adapter

This adapter will connect WhyceID to the WHYCEPOLICY engine
to enforce identity-based policies such as:

role restrictions  
trust score requirements  
cluster access policies  
guardian governance rules