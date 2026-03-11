# WHYCESPACE WBSM v3
# PHASE 2.0.6 — WHYCEID IDENTITY ACCESS SCOPE ENGINE

You are implementing **Phase 2.0.6 of the WhyceID System**.

This phase introduces the **Identity Access Scope Engine**, which restricts
permissions to specific resource scopes.

Access scopes enforce **multi-tenant boundaries** across the Whycespace system.

Example scopes:

cluster:whycemobility
cluster:whyceproperty
spv:taxi
spv:letting
system:global

Access scopes attach to **roles**, not directly to identities.

Identity → Roles → Permissions → Access Scope → Authorization

The engine must remain **stateless**.

Scope persistence is handled by the **IdentityAccessScopeStore**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless business logic

SYSTEM
stateful storage

The engine must:

• validate role existence
• validate scope format
• assign scopes to roles
• retrieve scopes
• prevent duplicate scope assignments

The engine must NOT persist data directly.

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityAccessScopeEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityAccessScopeStore.cs

Scope model:

src/system/upstream/WhyceID/models/

Create:

IdentityAccessScope.cs

---

# ACCESS SCOPE MODEL

Create:

models/IdentityAccessScope.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityAccessScope
(
    string Scope,
    DateTime AssignedAt
);

Validation rules:

Scope cannot be null
Scope cannot be empty

Scope format:

resource:value

Examples:

cluster:whyceproperty
cluster:whycemobility
spv:taxi
spv:letting
system:global

---

# ACCESS SCOPE STORE

Create:

stores/IdentityAccessScopeStore.cs

Purpose:

• store scopes assigned to roles
• retrieve scopes for roles
• thread-safe storage

Implementation:

using System.Collections.Concurrent;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityAccessScopeStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _scopes = new();

    public void Assign(string role, string scope)
    {
        var set = _scopes.GetOrAdd(role, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(scope);
        }
    }

    public IReadOnlyCollection<string> GetScopes(string role)
    {
        if (_scopes.TryGetValue(role, out var set))
        {
            return set.ToList();
        }

        return Array.Empty<string>();
    }

    public bool HasScope(string role, string scope)
    {
        if (_scopes.TryGetValue(role, out var set))
        {
            return set.Contains(scope);
        }

        return false;
    }
}

---

# ACCESS SCOPE ENGINE

Create:

src/engines/T0U/WhyceID/IdentityAccessScopeEngine.cs

Purpose:

• assign scopes to roles
• validate scope format
• prevent invalid assignments

Implementation:

using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityAccessScopeEngine
{
    private readonly IdentityAccessScopeStore _store;

    public IdentityAccessScopeEngine(
        IdentityAccessScopeStore store)
    {
        _store = store;
    }

    public void AssignScope(string role, string scope)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope cannot be empty.");
        }

        if (!scope.Contains(":"))
        {
            throw new ArgumentException(
                "Scope must follow format resource:value"
            );
        }

        _store.Assign(role, scope);
    }

    public IReadOnlyCollection<string> GetScopes(string role)
    {
        return _store.GetScopes(role);
    }

    public bool HasScope(string role, string scope)
    {
        return _store.HasScope(role, scope);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityAccessScopeEngineTests.cs

Test scenarios:

scope assigned successfully
duplicate scope ignored
empty role rejected
empty scope rejected
invalid scope format rejected
scopes retrievable
HasScope works correctly

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/roles/{role}/scopes

Returns:

role
list of scopes

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

2.0.7 Identity Verification Engine

This will integrate verification tiers and identity trust validation
into the WhyceID system.