# WHYCESPACE WBSM v3
# PHASE 2.0.5 — WHYCEID IDENTITY PERMISSION ENGINE

You are implementing **Phase 2.0.5 of the WhyceID System**.

This phase introduces the **Identity Permission Engine**, which maps
roles to permissions and enables fine-grained access control.

Permissions represent **actions an identity can perform**.

Examples:

cluster:create
cluster:delete
cluster:view
spv:create
spv:manage
vault:contribute
vault:withdraw
system:operate

Permissions are attached to **roles**, not directly to identities.

Identity → Roles → Permissions → Access Scope → Authorization

This engine must remain **stateless**.

Permission persistence is handled by the **IdentityPermissionStore**.

---

# ARCHITECTURE RULES

WBSM v3 doctrine:

ENGINE
stateless business logic

SYSTEM
stateful storage

The engine must:

• validate role existence
• validate permission format
• assign permissions to roles
• retrieve permissions
• prevent duplicate permission assignment

The engine must NOT persist data directly.

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityPermissionEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityPermissionStore.cs

Permission model:

src/system/upstream/WhyceID/models/

Create:

IdentityPermission.cs

---

# PERMISSION MODEL

Create:

models/IdentityPermission.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityPermission
(
    string Permission,
    DateTime AssignedAt
);

Validation rules:

Permission cannot be null
Permission cannot be empty

Permission format should follow:

resource:action

Examples:

cluster:create
spv:view
vault:contribute

---

# PERMISSION STORE

Create:

stores/IdentityPermissionStore.cs

Purpose:

• store permissions assigned to roles
• retrieve permissions for roles
• thread-safe storage

Implementation:

using System.Collections.Concurrent;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityPermissionStore
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _permissions = new();

    public void Assign(string role, string permission)
    {
        var set = _permissions.GetOrAdd(role, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(permission);
        }
    }

    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        if (_permissions.TryGetValue(role, out var set))
        {
            return set.ToList();
        }

        return Array.Empty<string>();
    }

    public bool HasPermission(string role, string permission)
    {
        if (_permissions.TryGetValue(role, out var set))
        {
            return set.Contains(permission);
        }

        return false;
    }
}

---

# PERMISSION ENGINE

Create:

src/engines/T0U/WhyceID/IdentityPermissionEngine.cs

Purpose:

• assign permissions to roles
• validate permission format
• prevent invalid assignments

Implementation:

using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityPermissionEngine
{
    private readonly IdentityPermissionStore _store;

    public IdentityPermissionEngine(
        IdentityPermissionStore store)
    {
        _store = store;
    }

    public void AssignPermission(string role, string permission)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission cannot be empty.");
        }

        if (!permission.Contains(":"))
        {
            throw new ArgumentException(
                "Permission must follow format resource:action"
            );
        }

        _store.Assign(role, permission);
    }

    public IReadOnlyCollection<string> GetPermissions(string role)
    {
        return _store.GetPermissions(role);
    }

    public bool HasPermission(string role, string permission)
    {
        return _store.HasPermission(role, permission);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityPermissionEngineTests.cs

Test scenarios:

permission assigned successfully
duplicate permission ignored
empty role rejected
empty permission rejected
invalid permission format rejected
permissions retrievable
HasPermission works correctly

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/roles/{role}/permissions

Returns:

role
list of permissions

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

2.0.6 Identity Access Scope Engine

Access scopes will introduce resource boundaries and multi-tenant
permission isolation across Whycespace clusters.