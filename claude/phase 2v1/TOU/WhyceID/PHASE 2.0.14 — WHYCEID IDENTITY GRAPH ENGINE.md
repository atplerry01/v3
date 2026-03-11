# WHYCESPACE WBSM v3
# PHASE 2.0.14 — WHYCEID IDENTITY GRAPH ENGINE

You are implementing **Phase 2.0.14 of the WhyceID System**.

This phase introduces the **Identity Graph Engine**, which manages
relationships between identities, organizations, and services.

The Identity Graph allows the system to model complex relationships
required for governance, delegation, and institutional structures.

Examples of relationships:

Identity → Identity (mentor, operator)
Identity → Organization (employee, member)
Identity → Service (service owner)
Identity → Cluster (cluster administrator)
Identity → SPV (spv operator)

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Graph relationships must be stored in **IdentityGraphStore**.

The engine must NOT persist state directly.

---

# GRAPH CONCEPT

A graph edge represents a relationship between two entities.

Edge properties:

edge id
source identity
target entity
relationship type
created timestamp

Relationship types examples:

guardian
operator
cluster_admin
spv_operator
service_owner
organization_member

Graph lifecycle:

create relationship
remove relationship
query relationships
query relationships by type

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityGraphEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityGraphStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityGraphEdge.cs

---

# GRAPH EDGE MODEL

Create:

models/IdentityGraphEdge.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityGraphEdge(
    Guid EdgeId,
    Guid SourceIdentityId,
    Guid TargetEntityId,
    string Relationship,
    DateTime CreatedAt
);

Validation rules:

EdgeId must not be empty
SourceIdentityId must not be empty
TargetEntityId must not be empty
Relationship must not be empty

---

# GRAPH STORE

Create:

stores/IdentityGraphStore.cs

Purpose:

store identity relationships
retrieve relationships
remove relationships
query relationship types

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityGraphStore
{
    private readonly ConcurrentDictionary<Guid, IdentityGraphEdge> _edges = new();

    public void Register(IdentityGraphEdge edge)
    {
        _edges[edge.EdgeId] = edge;
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetBySource(Guid identityId)
    {
        return _edges.Values
            .Where(e => e.SourceIdentityId == identityId)
            .ToList();
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetByRelationship(string relationship)
    {
        return _edges.Values
            .Where(e => e.Relationship == relationship)
            .ToList();
    }

    public void Remove(Guid edgeId)
    {
        _edges.TryRemove(edgeId, out _);
    }
}

---

# IDENTITY GRAPH ENGINE

Create:

src/engines/T0U/WhyceID/IdentityGraphEngine.cs

Dependencies:

IdentityRegistry
IdentityGraphStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityGraphEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityGraphStore _store;

    public IdentityGraphEngine(
        IdentityRegistry registry,
        IdentityGraphStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityGraphEdge CreateRelationship(
        Guid sourceIdentityId,
        Guid targetEntityId,
        string relationship)
    {
        if (!_registry.Exists(sourceIdentityId))
            throw new InvalidOperationException($"Identity does not exist: {sourceIdentityId}");

        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Relationship cannot be empty.");

        var edge = new IdentityGraphEdge(
            Guid.NewGuid(),
            sourceIdentityId,
            targetEntityId,
            relationship,
            DateTime.UtcNow
        );

        _store.Register(edge);

        return edge;
    }

    public void RemoveRelationship(Guid edgeId)
    {
        _store.Remove(edgeId);
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetRelationships(Guid identityId)
    {
        return _store.GetBySource(identityId);
    }

    public IReadOnlyCollection<IdentityGraphEdge> GetRelationshipsByType(string relationship)
    {
        return _store.GetByRelationship(relationship);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityGraphEngineTests.cs

Test scenarios:

create relationship successfully
missing identity rejected
relationship retrieval by source
relationship retrieval by type
relationship removal
multiple relationships supported

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/relationships

Returns relationships originating from identity.

Add endpoint:

POST /dev/identity/{id}/relationship

Input:

targetEntityId
relationship

Creates relationship.

Add endpoint:

POST /dev/relationship/remove

Input:

edgeId

Removes relationship.

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

2.0.15 Service Identity Engine

Service Identity will allow system components to have
secure machine identities.

Examples:

service runtime
workflow engine
cluster microservice
external integration identity