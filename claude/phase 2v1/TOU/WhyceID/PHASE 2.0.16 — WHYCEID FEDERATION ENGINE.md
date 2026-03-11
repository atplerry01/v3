# WHYCESPACE WBSM v3
# PHASE 2.0.16 — WHYCEID FEDERATION ENGINE

You are implementing **Phase 2.0.16 of the WhyceID System**.

This phase introduces the **Federation Engine**, which enables
WhyceID to accept identities from **external identity providers**.

Federation allows external authentication systems to integrate
securely with the Whycespace identity infrastructure.

Examples of federation providers:

enterprise SSO
OAuth providers
government identity systems
partner organizations
banking identity providers

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Federation mappings must be stored in **IdentityFederationStore**.

The engine must NOT persist state directly.

---

# FEDERATION CONCEPT

Federation maps an **external identity** to a **WhyceID identity**.

Federation properties:

federation id
external provider
external identity id
internal identity id
created timestamp
revoked flag

Federation lifecycle:

register federation
validate federation
retrieve federations
revoke federation

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

FederationEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityFederationStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityFederation.cs

---

# FEDERATION MODEL

Create:

models/IdentityFederation.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityFederation(
    Guid FederationId,
    string Provider,
    string ExternalIdentityId,
    Guid InternalIdentityId,
    DateTime CreatedAt,
    bool Revoked
);

Validation rules:

FederationId must not be empty
Provider must not be empty
ExternalIdentityId must not be empty
InternalIdentityId must not be empty

Example providers:

google-oauth
azure-ad
gov-identity
enterprise-sso

Example external identity ids:

email identifiers
provider subject identifiers
enterprise directory ids

---

# FEDERATION STORE

Create:

stores/IdentityFederationStore.cs

Purpose:

store federation mappings
retrieve mappings
validate federation identity
revoke federation

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityFederationStore
{
    private readonly ConcurrentDictionary<Guid, IdentityFederation> _federations = new();

    public void Register(IdentityFederation federation)
    {
        _federations[federation.FederationId] = federation;
    }

    public bool Validate(string provider, string externalIdentityId)
    {
        return _federations.Values.Any(f =>
            f.Provider == provider &&
            f.ExternalIdentityId == externalIdentityId &&
            !f.Revoked);
    }

    public IdentityFederation? Get(string provider, string externalIdentityId)
    {
        return _federations.Values.FirstOrDefault(f =>
            f.Provider == provider &&
            f.ExternalIdentityId == externalIdentityId &&
            !f.Revoked);
    }

    public void Revoke(Guid federationId)
    {
        if (_federations.TryGetValue(federationId, out var federation))
        {
            _federations[federationId] = federation with { Revoked = true };
        }
    }

    public IReadOnlyCollection<IdentityFederation> GetAll()
    {
        return _federations.Values.ToList();
    }
}

---

# FEDERATION ENGINE

Create:

src/engines/T0U/WhyceID/FederationEngine.cs

Dependencies:

IdentityRegistry
IdentityFederationStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class FederationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityFederationStore _store;

    public FederationEngine(
        IdentityRegistry registry,
        IdentityFederationStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityFederation RegisterFederation(
        string provider,
        string externalIdentityId,
        Guid internalIdentityId)
    {
        if (!_registry.Exists(internalIdentityId))
            throw new InvalidOperationException($"Identity does not exist: {internalIdentityId}");

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty.");

        if (string.IsNullOrWhiteSpace(externalIdentityId))
            throw new ArgumentException("External identity id cannot be empty.");

        var federation = new IdentityFederation(
            Guid.NewGuid(),
            provider,
            externalIdentityId,
            internalIdentityId,
            DateTime.UtcNow,
            false
        );

        _store.Register(federation);

        return federation;
    }

    public bool ValidateFederation(string provider, string externalIdentityId)
    {
        return _store.Validate(provider, externalIdentityId);
    }

    public IdentityFederation? GetFederation(string provider, string externalIdentityId)
    {
        return _store.Get(provider, externalIdentityId);
    }

    public void RevokeFederation(Guid federationId)
    {
        _store.Revoke(federationId);
    }

    public IReadOnlyCollection<IdentityFederation> GetFederations()
    {
        return _store.GetAll();
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

FederationEngineTests.cs

Test scenarios:

register federation successfully
missing identity rejected
validate federation success
validate federation failure
revoke federation successfully
retrieve federation mappings

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/federations

Returns all federation mappings.

Add endpoint:

POST /dev/federation/register

Input:

provider
externalIdentityId
internalIdentityId

Registers federation mapping.

Add endpoint:

POST /dev/federation/revoke

Input:

federationId

Revokes federation mapping.

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

2.0.17 Identity Recovery Engine

Identity Recovery will allow secure restoration of identity access
through controlled recovery processes such as:

guardian approval
multi-factor recovery
verified recovery flows