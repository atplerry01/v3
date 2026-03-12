# WHYCESPACE WBSM v3
# PHASE 2.0.11 — WHYCEID AUTHORIZATION ENGINE

You are implementing **Phase 2.0.11 of the WhyceID System**.

This phase introduces the **Authorization Engine**, which determines
whether an identity is allowed to perform a specific action.

Authorization evaluates:

identity roles
role permissions
role scopes

The engine must remain **stateless**.

Authorization will operate after **successful authentication**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Authorization must NOT persist data.

Authorization only **evaluates access decisions**.

---

# AUTHORIZATION MODEL

Authorization evaluates:

Identity
Role
Permission
Scope

Decision result:

ALLOW
DENY

Example request:

IdentityId: 123
Resource: cluster
Action: update
Scope: cluster:whyceproperty

Permission format:

resource:action

Example:

cluster:update
spv:create
vault:withdraw

---

# TARGET LOCATION

Engine:

src/engines/T0U/WhyceID/

Create:

AuthorizationEngine.cs

Models:

src/system/upstream/WhyceID/models/

Create:

AuthorizationRequest.cs
AuthorizationResult.cs

---

# AUTHORIZATION REQUEST MODEL

Create:

models/AuthorizationRequest.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record AuthorizationRequest(
    Guid IdentityId,
    string Resource,
    string Action,
    string Scope
);

---

# AUTHORIZATION RESULT MODEL

Create:

models/AuthorizationResult.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record AuthorizationResult(
    bool Allowed,
    string Reason
);

---

# AUTHORIZATION ENGINE

Create:

src/engines/T0U/WhyceID/AuthorizationEngine.cs

Dependencies:

IdentityRegistry
IdentityRoleEngine
IdentityPermissionEngine
IdentityAccessScopeEngine

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class AuthorizationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleEngine _roleEngine;
    private readonly IdentityPermissionEngine _permissionEngine;
    private readonly IdentityAccessScopeEngine _scopeEngine;

    public AuthorizationEngine(
        IdentityRegistry registry,
        IdentityRoleEngine roleEngine,
        IdentityPermissionEngine permissionEngine,
        IdentityAccessScopeEngine scopeEngine)
    {
        _registry = registry;
        _roleEngine = roleEngine;
        _permissionEngine = permissionEngine;
        _scopeEngine = scopeEngine;
    }

    public AuthorizationResult Authorize(AuthorizationRequest request)
    {
        if (!_registry.Exists(request.IdentityId))
        {
            return new AuthorizationResult(false, "Identity does not exist");
        }

        var roles = _roleEngine.GetRoles(request.IdentityId);

        var permission = $"{request.Resource}:{request.Action}";

        foreach (var role in roles)
        {
            var hasPermission = _permissionEngine.HasPermission(role, permission);

            if (!hasPermission)
                continue;

            var hasScope = _scopeEngine.HasScope(role, request.Scope);

            if (!hasScope)
                continue;

            return new AuthorizationResult(true, "Authorized");
        }

        return new AuthorizationResult(false, "Permission denied");
    }
}

---

# AUTHORIZATION RULES

Authorization succeeds when:

identity exists
role contains permission
role contains scope

Otherwise:

access denied.

---

# TESTING

Create tests:

tests/engines/whyceid/

AuthorizationEngineTests.cs

Test scenarios:

identity with valid role/permission/scope allowed
missing identity rejected
missing permission denied
missing scope denied
multiple roles evaluated correctly
authorization success with correct role

---

# DEBUG ENDPOINT

Add debug endpoint:

POST /dev/authorize

Input:

identityId
resource
action
scope

Returns:

authorization decision

Only available in DEBUG mode.

---

# SUCCESS CRITERIA

Build must succeed.

Requirements:

0 warnings
0 errors
all tests passing

Engine must remain stateless.

---

# NEXT PHASE

After this phase implement:

2.0.12 Session Engine

The Session Engine will manage authenticated sessions and
secure access tokens across Whycespace systems.