# WHYCESPACE WBSM v3
# PHASE 2.0.17 — WHYCEID IDENTITY RECOVERY ENGINE

You are implementing **Phase 2.0.17 of the WhyceID System**.

This phase introduces the **Identity Recovery Engine**, which manages
secure identity recovery workflows when identities lose access to
their authentication credentials or trusted devices.

Recovery allows controlled restoration of identity access.

Examples:

lost device recovery
credential compromise recovery
operator-assisted recovery
guardian-approved recovery

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Recovery records must be stored in **IdentityRecoveryStore**.

The engine must NOT persist state directly.

---

# RECOVERY CONCEPT

Recovery represents a request to restore identity access.

Recovery properties:

recovery id
identity id
reason
status
created timestamp
completed timestamp

Recovery statuses:

pending
approved
rejected
completed

Recovery lifecycle:

create recovery request
approve recovery
reject recovery
complete recovery
retrieve recovery requests

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityRecoveryEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityRecoveryStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityRecovery.cs

---

# RECOVERY MODEL

Create:

models/IdentityRecovery.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityRecovery(
    Guid RecoveryId,
    Guid IdentityId,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

Validation rules:

RecoveryId must not be empty
IdentityId must not be empty
Reason must not be empty
Status must not be empty

Example reasons:

lost_device
forgot_credentials
security_compromise

Example statuses:

pending
approved
rejected
completed

---

# RECOVERY STORE

Create:

stores/IdentityRecoveryStore.cs

Purpose:

store recovery requests
retrieve recovery requests
update recovery status
track recovery history

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityRecoveryStore
{
    private readonly ConcurrentDictionary<Guid, IdentityRecovery> _recoveries = new();

    public void Register(IdentityRecovery recovery)
    {
        _recoveries[recovery.RecoveryId] = recovery;
    }

    public IdentityRecovery Get(Guid recoveryId)
    {
        if (!_recoveries.TryGetValue(recoveryId, out var recovery))
            throw new KeyNotFoundException($"Recovery not found: {recoveryId}");

        return recovery;
    }

    public IReadOnlyCollection<IdentityRecovery> GetByIdentity(Guid identityId)
    {
        return _recoveries.Values
            .Where(r => r.IdentityId == identityId)
            .ToList();
    }

    public void Update(IdentityRecovery recovery)
    {
        _recoveries[recovery.RecoveryId] = recovery;
    }

    public IReadOnlyCollection<IdentityRecovery> GetAll()
    {
        return _recoveries.Values.ToList();
    }
}

---

# IDENTITY RECOVERY ENGINE

Create:

src/engines/T0U/WhyceID/IdentityRecoveryEngine.cs

Dependencies:

IdentityRegistry
IdentityRecoveryStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityRecoveryEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRecoveryStore _store;

    public IdentityRecoveryEngine(
        IdentityRegistry registry,
        IdentityRecoveryStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityRecovery CreateRecovery(Guid identityId, string reason)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Recovery reason cannot be empty.");

        var recovery = new IdentityRecovery(
            Guid.NewGuid(),
            identityId,
            reason,
            "pending",
            DateTime.UtcNow,
            null
        );

        _store.Register(recovery);

        return recovery;
    }

    public void ApproveRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);

        var updated = recovery with { Status = "approved" };

        _store.Update(updated);
    }

    public void RejectRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);

        var updated = recovery with { Status = "rejected" };

        _store.Update(updated);
    }

    public void CompleteRecovery(Guid recoveryId)
    {
        var recovery = _store.Get(recoveryId);

        var updated = recovery with
        {
            Status = "completed",
            CompletedAt = DateTime.UtcNow
        };

        _store.Update(updated);
    }

    public IReadOnlyCollection<IdentityRecovery> GetRecoveries(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityRecovery> GetAllRecoveries()
    {
        return _store.GetAll();
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityRecoveryEngineTests.cs

Test scenarios:

create recovery successfully
missing identity rejected
approve recovery
reject recovery
complete recovery
retrieve identity recoveries

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/recoveries

Returns recovery requests for identity.

Add endpoint:

POST /dev/recovery/create

Input:

identityId
reason

Creates recovery request.

Add endpoint:

POST /dev/recovery/approve

Input:

recoveryId

Approves recovery.

Add endpoint:

POST /dev/recovery/reject

Input:

recoveryId

Rejects recovery.

Add endpoint:

POST /dev/recovery/complete

Input:

recoveryId

Completes recovery.

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

2.0.18 Identity Revocation Engine

Revocation will allow identities to be suspended or revoked
due to security violations, governance actions, or policy enforcement.