# WHYCESPACE WBSM v3
# PHASE 2.0.4 — WHYCEID IDENTITY ROLE ENGINE

You are implementing **Phase 2.0.4 of the WhyceID System**.

This phase introduces the **Identity Role Engine**, which provides
Role-Based Access Control (RBAC) capabilities.

Roles represent **identity privileges** within the Whycespace ecosystem.

Examples of roles:

Admin
Operator
ClusterAdmin
ClusterProvider
Guardian
Developer
Auditor

Roles will later map to:

Permissions
Access Scopes
Policy Enforcement

The role engine must remain **stateless**.

Role storage will be handled by the **Identity Role Store**.

---

# ARCHITECTURE RULES

WBSM v3 doctrine:

ENGINE
stateless business logic

SYSTEM
stateful storage

The role engine must:

• validate role assignment
• verify identity existence
• prevent duplicate role assignments
• delegate persistence to the Role Store

The engine must NOT persist data directly.

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityRoleEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityRoleStore.cs

Role model:

src/system/upstream/WhyceID/models/

Create:

IdentityRole.cs

---

# ROLE MODEL

Create:

models/IdentityRole.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityRole
(
    string RoleName,
    DateTime AssignedAt
);

Validation rules:

RoleName cannot be null
RoleName cannot be empty

---

# ROLE STORE

Create:

stores/IdentityRoleStore.cs

Purpose:

• store roles assigned to identities
• allow role lookup
• prevent duplicates
• thread-safe storage

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityRoleStore
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _roles = new();

    public void Assign(Guid identityId, string role)
    {
        var set = _roles.GetOrAdd(identityId, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(role);
        }
    }

    public IReadOnlyCollection<string> GetRoles(Guid identityId)
    {
        if (_roles.TryGetValue(identityId, out var roles))
        {
            return roles.ToList();
        }

        return Array.Empty<string>();
    }

    public bool HasRole(Guid identityId, string role)
    {
        if (_roles.TryGetValue(identityId, out var roles))
        {
            return roles.Contains(role);
        }

        return false;
    }
}

---

# ROLE ENGINE

Create:

src/engines/T0U/WhyceID/IdentityRoleEngine.cs

Purpose:

• validate role assignment
• ensure identity exists
• prevent invalid roles

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityRoleEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _store;

    public IdentityRoleEngine(
        IdentityRegistry registry,
        IdentityRoleStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void AssignRole(Guid identityId, string role)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}"
            );
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Role cannot be empty.");
        }

        _store.Assign(identityId, role);
    }

    public IReadOnlyCollection<string> GetRoles(Guid identityId)
    {
        return _store.GetRoles(identityId);
    }

    public bool HasRole(Guid identityId, string role)
    {
        return _store.HasRole(identityId, role);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityRoleEngineTests.cs

Test scenarios:

role assigned successfully
duplicate role ignored
missing identity rejected
empty role rejected
multiple roles supported
roles retrievable
HasRole works correctly

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/roles

Returns:

identity id
list of roles

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

2.0.5 Identity Permission Engine

Permissions will attach to roles and enable
fine-grained access control across Whycespace.s