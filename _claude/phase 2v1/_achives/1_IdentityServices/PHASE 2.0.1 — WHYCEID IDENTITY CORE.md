# WHYCESPACE WBSM v3

# PHASE 2.0.1 — WHYCEID IDENTITY CORE
(System + Engine Architecture)

You are implementing the **core identity domain of WhyceID**.

WhyceID is the **constitutional identity system of Whycespace**.

Every participant, provider, operator, guardian, and system service must possess an identity registered in this system.

This module implements the **identity state layer (SYSTEM)** and **identity execution layer (ENGINE)**.

Follow the WBSM architecture rules:

SYSTEM → state, aggregates, registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• IdentityAggregate  
• IdentityRegistry  

ENGINE COMPONENT

• IdentityCreationEngine  
• IdentityVerificationEngine  

Also implement:

• Commands  
• Events  
• Unit tests  

---

# SYSTEM MODULE LOCATION

Create:

```
src/system/upstream/WhyceID/
```

Structure:

```
WhyceID/

├── aggregates/
├── registry/
├── models/
├── commands/
└── events/
```

Project:

```
Whycespace.System.WhyceID.csproj
```

Target framework:

```
net8.0
```

---

# ENGINE MODULE LOCATION

Create:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

Structure:

```
WhyceIdentity/

├── IdentityCreationEngine.cs
└── IdentityVerificationEngine.cs
```

Project:

```
Whycespace.Engine.Identity.csproj
```

References:

```
Whycespace.System.WhyceID
Whycespace.Engine.Abstractions
Whycespace.Contracts
```

---

# MODELS

Create:

```
models/IdentityId.cs
```

```csharp
public readonly record struct IdentityId(Guid Value);
```

---

Create:

```
models/IdentityType.cs
```

```csharp
public enum IdentityType
{
    Individual = 1,
    Organization = 2,
    SystemService = 3
}
```

---

Create:

```
models/IdentityStatus.cs
```

```csharp
public enum IdentityStatus
{
    PendingVerification = 1,
    Active = 2,
    Suspended = 3,
    Revoked = 4
}
```

---

# AGGREGATE

Create:

```
aggregates/IdentityAggregate.cs
```

```csharp
public sealed class IdentityAggregate
{
    public IdentityId IdentityId { get; }

    public IdentityType Type { get; }

    public IdentityStatus Status { get; private set; }

    public DateTime CreatedAt { get; }

    public IdentityAggregate(
        IdentityId identityId,
        IdentityType type,
        DateTime createdAt)
    {
        IdentityId = identityId;
        Type = type;
        CreatedAt = createdAt;
        Status = IdentityStatus.PendingVerification;
    }

    public void Activate()
    {
        if (Status != IdentityStatus.PendingVerification)
            throw new InvalidOperationException("Identity cannot be activated");

        Status = IdentityStatus.Active;
    }

    public void Suspend()
    {
        if (Status != IdentityStatus.Active)
            throw new InvalidOperationException("Only active identity may be suspended");

        Status = IdentityStatus.Suspended;
    }
}
```

---

# REGISTRY

Create:

```
registry/IdentityRegistry.cs
```

```csharp
public sealed class IdentityRegistry
{
    private readonly ConcurrentDictionary<Guid, IdentityAggregate> _identities
        = new();

    public bool Exists(Guid id)
    {
        return _identities.ContainsKey(id);
    }

    public void Register(IdentityAggregate identity)
    {
        if (!_identities.TryAdd(identity.IdentityId.Value, identity))
            throw new InvalidOperationException("Identity already exists");
    }

    public IdentityAggregate Get(Guid id)
    {
        if (!_identities.TryGetValue(id, out var identity))
            throw new KeyNotFoundException("Identity not found");

        return identity;
    }
}
```

---

# COMMANDS

Create:

```
commands/CreateIdentityCommand.cs
```

```csharp
public sealed record CreateIdentityCommand(
    Guid IdentityId,
    IdentityType Type);
```

---

Create:

```
commands/VerifyIdentityCommand.cs
```

```csharp
public sealed record VerifyIdentityCommand(
    Guid IdentityId);
```

---

# EVENTS

Create:

```
events/IdentityCreatedEvent.cs
```

```csharp
public sealed record IdentityCreatedEvent(
    Guid IdentityId,
    IdentityType Type,
    DateTime CreatedAt);
```

---

Create:

```
events/IdentityVerifiedEvent.cs
```

```csharp
public sealed record IdentityVerifiedEvent(
    Guid IdentityId,
    DateTime VerifiedAt);
```

---

# ENGINE IMPLEMENTATION

Create:

```
engines/T0U_Constitutional/WhyceIdentity/IdentityCreationEngine.cs
```

```csharp
public sealed class IdentityCreationEngine
{
    public IdentityCreatedEvent Execute(
        CreateIdentityCommand command,
        IdentityRegistry registry)
    {
        if (registry.Exists(command.IdentityId))
            throw new InvalidOperationException("Identity already exists");

        var identity = new IdentityAggregate(
            new IdentityId(command.IdentityId),
            command.Type,
            DateTime.UtcNow);

        registry.Register(identity);

        return new IdentityCreatedEvent(
            command.IdentityId,
            command.Type,
            identity.CreatedAt);
    }
}
```

---

Create:

```
engines/T0U_Constitutional/WhyceIdentity/IdentityVerificationEngine.cs
```

```csharp
public sealed class IdentityVerificationEngine
{
    public IdentityVerifiedEvent Execute(
        VerifyIdentityCommand command,
        IdentityRegistry registry)
    {
        var identity = registry.Get(command.IdentityId);

        identity.Activate();

        return new IdentityVerifiedEvent(
            command.IdentityId,
            DateTime.UtcNow);
    }
}
```

---

# UNIT TESTS

Create test project:

```
tests/Whycespace.Identity.Tests/
```

Tests:

```
IdentityCreationTests
IdentityVerificationTests
IdentityRegistryTests
```

Example:

```csharp
[Fact]
public void IdentityCreation_ShouldRegisterIdentity()
{
    var registry = new IdentityRegistry();

    var engine = new IdentityCreationEngine();

    var cmd = new CreateIdentityCommand(Guid.NewGuid(), IdentityType.Individual);

    var result = engine.Execute(cmd, registry);

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

Identity creation works  
Identity verification works  
Registry prevents duplicates  
Events emitted correctly  
Unit tests pass  

---

# END OF PHASE 2.0.1