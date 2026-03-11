# WHYCESPACE WBSM v3
# PHASE 2.0.20 — WHYCEID IDENTITY AUDIT ENGINE

You are implementing **Phase 2.0.20 of the WhyceID System**.

This phase introduces the **Identity Audit Engine**, which records
identity-related actions across the system for security, governance,
and compliance purposes.

Audit logs provide a traceable history of identity events.

Examples of events:

authentication attempts
authorization decisions
role assignments
device registrations
policy evaluations
identity revocations
recovery operations

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE  
stateless logic

SYSTEM  
stateful storage

Audit events must be stored in **IdentityAuditStore**.

The engine must NOT persist state directly.

---

# AUDIT EVENT CONCEPT

An audit event records an identity action.

Audit event properties:

event id  
identity id  
event type  
description  
timestamp  

Examples of event types:

authentication_attempt  
authorization_decision  
role_assignment  
device_registration  
policy_evaluation  
identity_revocation  
identity_recovery  

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityAuditEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityAuditStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityAuditEvent.cs

---

# AUDIT EVENT MODEL

Create:

models/IdentityAuditEvent.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityAuditEvent(
    Guid EventId,
    Guid IdentityId,
    string EventType,
    string Description,
    DateTime Timestamp
);

Validation rules:

EventId must not be empty  
IdentityId must not be empty  
EventType must not be empty  

---

# AUDIT STORE

Create:

stores/IdentityAuditStore.cs

Purpose:

store audit events  
retrieve identity audit history  
retrieve global audit events  

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityAuditStore
{
    private readonly ConcurrentDictionary<Guid, IdentityAuditEvent> _events = new();

    public void Register(IdentityAuditEvent auditEvent)
    {
        _events[auditEvent.EventId] = auditEvent;
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetByIdentity(Guid identityId)
    {
        return _events.Values
            .Where(e => e.IdentityId == identityId)
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetAll()
    {
        return _events.Values
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }
}

---

# IDENTITY AUDIT ENGINE

Create:

src/engines/T0U/WhyceID/IdentityAuditEngine.cs

Dependencies:

IdentityRegistry  
IdentityAuditStore  

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityAuditEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAuditStore _store;

    public IdentityAuditEngine(
        IdentityRegistry registry,
        IdentityAuditStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityAuditEvent RecordEvent(
        Guid identityId,
        string eventType,
        string description)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty.");

        var auditEvent = new IdentityAuditEvent(
            Guid.NewGuid(),
            identityId,
            eventType,
            description ?? "",
            DateTime.UtcNow
        );

        _store.Register(auditEvent);

        return auditEvent;
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetIdentityAudit(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityAuditEvent> GetAllAuditEvents()
    {
        return _store.GetAll();
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityAuditEngineTests.cs

Test scenarios:

record audit event successfully  
missing identity rejected  
retrieve identity audit history  
retrieve global audit history  
multiple events recorded  

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/audit

Returns audit history for identity.

Add endpoint:

POST /dev/identity/audit

Input:

identityId  
eventType  
description  

Records audit event.

Add endpoint:

GET /dev/audit

Returns all audit events.

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

# NEXT SYSTEM

After completing Phase 2.0.20 the **WhyceID System is complete**.

Next major system implementation will continue with:

WHYCEPOLICY ENGINE  
WHYCECHAIN ENGINE  
HEOS / WSS orchestration integration