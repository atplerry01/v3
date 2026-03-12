# WHYCESPACE WBSM v3

# PHASE 2.0.4 — WHYCEID AUTHORIZATION ENGINE
(System + Engine Architecture)

You are implementing the **authorization subsystem of WhyceID**.

Authorization determines **whether an identity has permission to perform an action**.

This module introduces:

• roles  
• permissions  
• role assignments  
• permission evaluation  

This module implements a **role-based access control (RBAC)** system.

Follow WBSM architecture rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• RoleDefinition  
• PermissionDefinition  
• RoleRegistry  
• RoleAssignmentRegistry  

ENGINE COMPONENT

• RoleAssignmentEngine  
• AuthorizationEngine  

Also implement:

• Commands  
• Events  
• Unit tests  

---

# SYSTEM MODULE LOCATION

Extend:

```
src/system/upstream/WhyceID/
```

Create folder:

```
authorization/
```

Structure:

```
authorization/

├── RoleDefinition.cs
├── PermissionDefinition.cs
├── RoleRegistry.cs
├── RoleAssignmentRegistry.cs
```

---

# ROLE DEFINITION

Create:

```
authorization/RoleDefinition.cs
```

```csharp
public sealed class RoleDefinition
{
    public string RoleName { get; }

    public IReadOnlyCollection<string> Permissions { get; }

    public RoleDefinition(string roleName, IEnumerable<string> permissions)
    {
        RoleName = roleName;
        Permissions = permissions.ToList().AsReadOnly();
    }
}
```

---

# PERMISSION DEFINITION

Create:

```
authorization/PermissionDefinition.cs
```

```csharp
public sealed class PermissionDefinition
{
    public string Name { get; }

    public PermissionDefinition(string name)
    {
        Name = name;
    }
}
```

---

# ROLE REGISTRY

Create:

```
authorization/RoleRegistry.cs
```

```csharp
public sealed class RoleRegistry
{
    private readonly ConcurrentDictionary<string, RoleDefinition> _roles
        = new();

    public void Register(RoleDefinition role)
    {
        if (!_roles.TryAdd(role.RoleName, role))
            throw new InvalidOperationException("Role already exists");
    }

    public RoleDefinition Get(string roleName)
    {
        if (!_roles.TryGetValue(roleName, out var role))
            throw new KeyNotFoundException("Role not found");

        return role;
    }
}
```

---

# ROLE ASSIGNMENT REGISTRY

Create:

```
authorization/RoleAssignmentRegistry.cs
```

```csharp
public sealed class RoleAssignmentRegistry
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _assignments
        = new();

    public void AssignRole(Guid identityId, string role)
    {
        var set = _assignments.GetOrAdd(identityId, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(role);
        }
    }

    public IReadOnlyCollection<string> GetRoles(Guid identityId)
    {
        if (!_assignments.TryGetValue(identityId, out var roles))
            return Array.Empty<string>();

        return roles.ToList().AsReadOnly();
    }
}
```

---

# COMMANDS

Create:

```
commands/AssignRoleCommand.cs
```

```csharp
public sealed record AssignRoleCommand(
    Guid IdentityId,
    string RoleName);
```

---

Create:

```
commands/AuthorizeCommand.cs
```

```csharp
public sealed record AuthorizeCommand(
    Guid IdentityId,
    string Permission);
```

---

# EVENTS

Create:

```
events/RoleAssignedEvent.cs
```

```csharp
public sealed record RoleAssignedEvent(
    Guid IdentityId,
    string RoleName,
    DateTime AssignedAt);
```

---

Create:

```
events/AuthorizationSucceededEvent.cs
```

```csharp
public sealed record AuthorizationSucceededEvent(
    Guid IdentityId,
    string Permission,
    DateTime AuthorizedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# ROLE ASSIGNMENT ENGINE

Create:

```
RoleAssignmentEngine.cs
```

```csharp
public sealed class RoleAssignmentEngine
{
    public RoleAssignedEvent Execute(
        AssignRoleCommand command,
        RoleAssignmentRegistry registry)
    {
        registry.AssignRole(command.IdentityId, command.RoleName);

        return new RoleAssignedEvent(
            command.IdentityId,
            command.RoleName,
            DateTime.UtcNow);
    }
}
```

---

# AUTHORIZATION ENGINE

Create:

```
AuthorizationEngine.cs
```

```csharp
public sealed class AuthorizationEngine
{
    public AuthorizationSucceededEvent Execute(
        AuthorizeCommand command,
        RoleAssignmentRegistry assignments,
        RoleRegistry roles)
    {
        var roleNames = assignments.GetRoles(command.IdentityId);

        foreach (var roleName in roleNames)
        {
            var role = roles.Get(roleName);

            if (role.Permissions.Contains(command.Permission))
            {
                return new AuthorizationSucceededEvent(
                    command.IdentityId,
                    command.Permission,
                    DateTime.UtcNow);
            }
        }

        throw new UnauthorizedAccessException("Permission denied");
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.Authorization.Tests/
```

Tests:

```
RoleRegistryTests
RoleAssignmentTests
AuthorizationEngineTests
```

Example:

```csharp
[Fact]
public void Authorization_ShouldSucceed_WhenRoleHasPermission()
{
    var roleRegistry = new RoleRegistry();
    var assignmentRegistry = new RoleAssignmentRegistry();

    roleRegistry.Register(
        new RoleDefinition("admin", new[] { "cluster.create" }));

    var identity = Guid.NewGuid();

    assignmentRegistry.AssignRole(identity, "admin");

    var engine = new AuthorizationEngine();

    var result = engine.Execute(
        new AuthorizeCommand(identity, "cluster.create"),
        assignmentRegistry,
        roleRegistry);

    Assert.NotNull(result);
}
```

---

# BUILD VALIDATION

Run:

```
dotnet build
```

Expected result:

```
Build succeeded
0 errors
0 warnings
```

---

# SUCCESS CRITERIA

Roles can be registered  
Roles can be assigned to identities  
Authorization checks permissions  
Unauthorized access throws exception  
Unit tests pass  

---

# END OF PHASE 2.0.4