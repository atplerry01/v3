# WHYCESPACE WBSM v3
# PHASE 2.0.13 — WHYCEID CONSENT ENGINE

You are implementing **Phase 2.0.13 of the WhyceID System**.

This phase introduces the **Consent Engine**, which manages
identity permissions for systems, services, and integrations
to access identity-related data or functionality.

Consent ensures that identities explicitly approve system access.

This capability is essential for:

data protection
third-party integrations
audit compliance
system transparency

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Consent data must be stored in **IdentityConsentStore**.

The engine must NOT persist state directly.

---

# CONSENT CONCEPT

Consent represents permission granted by an identity.

Consent properties:

consent id
identity id
target system
permission scope
granted timestamp
revoked flag

Consent lifecycle:

grant consent
check consent
revoke consent
list consents

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

ConsentEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityConsentStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityConsent.cs

---

# CONSENT MODEL

Create:

models/IdentityConsent.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityConsent(
    Guid ConsentId,
    Guid IdentityId,
    string Target,
    string Scope,
    DateTime GrantedAt,
    bool Revoked
);

Validation rules:

ConsentId must not be empty
IdentityId must not be empty
Target must not be empty
Scope must not be empty

Example values:

Target:

whyceproperty
whycemobility
externalbankapi

Scope:

identity:read
identity:profile
identity:financial

---

# CONSENT STORE

Create:

stores/IdentityConsentStore.cs

Purpose:

store identity consents
retrieve identity consents
revoke consents
validate access permissions

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityConsentStore
{
    private readonly ConcurrentDictionary<Guid, IdentityConsent> _consents = new();

    public void Register(IdentityConsent consent)
    {
        _consents[consent.ConsentId] = consent;
    }

    public IReadOnlyCollection<IdentityConsent> GetByIdentity(Guid identityId)
    {
        return _consents.Values
            .Where(c => c.IdentityId == identityId)
            .ToList();
    }

    public bool HasConsent(Guid identityId, string target, string scope)
    {
        return _consents.Values.Any(c =>
            c.IdentityId == identityId &&
            c.Target == target &&
            c.Scope == scope &&
            !c.Revoked);
    }

    public void Revoke(Guid consentId)
    {
        if (_consents.TryGetValue(consentId, out var consent))
        {
            _consents[consentId] = consent with { Revoked = true };
        }
    }
}

---

# CONSENT ENGINE

Create:

src/engines/T0U/WhyceID/ConsentEngine.cs

Dependencies:

IdentityRegistry
IdentityConsentStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class ConsentEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityConsentStore _store;

    public ConsentEngine(
        IdentityRegistry registry,
        IdentityConsentStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityConsent GrantConsent(
        Guid identityId,
        string target,
        string scope)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be empty.");

        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be empty.");

        var consent = new IdentityConsent(
            Guid.NewGuid(),
            identityId,
            target,
            scope,
            DateTime.UtcNow,
            false
        );

        _store.Register(consent);

        return consent;
    }

    public bool CheckConsent(Guid identityId, string target, string scope)
    {
        return _store.HasConsent(identityId, target, scope);
    }

    public void RevokeConsent(Guid consentId)
    {
        _store.Revoke(consentId);
    }

    public IReadOnlyCollection<IdentityConsent> GetConsents(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

ConsentEngineTests.cs

Test scenarios:

grant consent successfully
missing identity rejected
consent check success
consent revoked successfully
multiple consents supported
identity consent listing

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/consents

Returns identity consents.

Add endpoint:

POST /dev/identity/{id}/consent

Input:

target
scope

Returns granted consent.

Add endpoint:

POST /dev/consent/revoke

Input:

consentId

Revokes consent.

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

2.0.14 Identity Graph Engine

Identity Graph will manage relationships between identities:

identity → identity
identity → organization
identity → service

This becomes the foundation for advanced identity relationships
across the Whycespace ecosystem.