# WHYCESPACE WBSM v3

# PHASE 2.0.2 — WHYCEID IDENTITY VERIFICATION SYSTEM
(System + Engine Architecture)

You are implementing the **identity verification subsystem** of WhyceID.

This subsystem manages:

• identity attributes  
• verification records  
• verification levels  
• identity proofing  

The goal is to allow identities to be verified to different **trust levels**.

Example:

```
Level 0 → Unverified
Level 1 → Email verified
Level 2 → Government ID verified
Level 3 → Institutional verified
```

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• IdentityAttribute  
• IdentityAttributeRegistry  
• IdentityVerificationRecord  
• IdentityVerificationRegistry  

ENGINE COMPONENT

• IdentityAttributeEngine  
• IdentityVerificationEngine  

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

Add folders:

```
WhyceID/

├── attributes/
├── verification/
├── commands/
└── events/
```

---

# VERIFICATION LEVEL

Create:

```
verification/VerificationLevel.cs
```

```csharp
public enum VerificationLevel
{
    None = 0,
    Basic = 1,
    GovernmentId = 2,
    Institutional = 3
}
```

---

# ATTRIBUTE MODEL

Create:

```
attributes/IdentityAttribute.cs
```

```csharp
public sealed class IdentityAttribute
{
    public Guid IdentityId { get; }

    public string Key { get; }

    public string Value { get; }

    public DateTime CreatedAt { get; }

    public IdentityAttribute(Guid identityId, string key, string value)
    {
        IdentityId = identityId;
        Key = key;
        Value = value;
        CreatedAt = DateTime.UtcNow;
    }
}
```

---

# ATTRIBUTE REGISTRY

Create:

```
attributes/IdentityAttributeRegistry.cs
```

```csharp
public sealed class IdentityAttributeRegistry
{
    private readonly ConcurrentDictionary<Guid, List<IdentityAttribute>> _attributes
        = new();

    public void AddAttribute(IdentityAttribute attribute)
    {
        var list = _attributes.GetOrAdd(attribute.IdentityId, _ => new List<IdentityAttribute>());

        lock (list)
        {
            list.Add(attribute);
        }
    }

    public IReadOnlyList<IdentityAttribute> GetAttributes(Guid identityId)
    {
        if (!_attributes.TryGetValue(identityId, out var list))
            return Array.Empty<IdentityAttribute>();

        return list.AsReadOnly();
    }
}
```

---

# VERIFICATION RECORD

Create:

```
verification/IdentityVerificationRecord.cs
```

```csharp
public sealed class IdentityVerificationRecord
{
    public Guid IdentityId { get; }

    public VerificationLevel Level { get; private set; }

    public DateTime VerifiedAt { get; private set; }

    public IdentityVerificationRecord(Guid identityId)
    {
        IdentityId = identityId;
        Level = VerificationLevel.None;
    }

    public void Upgrade(VerificationLevel level)
    {
        if (level <= Level)
            throw new InvalidOperationException("Verification level must increase");

        Level = level;
        VerifiedAt = DateTime.UtcNow;
    }
}
```

---

# VERIFICATION REGISTRY

Create:

```
verification/IdentityVerificationRegistry.cs
```

```csharp
public sealed class IdentityVerificationRegistry
{
    private readonly ConcurrentDictionary<Guid, IdentityVerificationRecord> _records
        = new();

    public IdentityVerificationRecord GetOrCreate(Guid identityId)
    {
        return _records.GetOrAdd(identityId, id => new IdentityVerificationRecord(id));
    }

    public IdentityVerificationRecord Get(Guid identityId)
    {
        if (!_records.TryGetValue(identityId, out var record))
            throw new KeyNotFoundException("Verification record not found");

        return record;
    }
}
```

---

# COMMANDS

Create:

```
commands/AddIdentityAttributeCommand.cs
```

```csharp
public sealed record AddIdentityAttributeCommand(
    Guid IdentityId,
    string Key,
    string Value);
```

---

Create:

```
commands/UpgradeVerificationCommand.cs
```

```csharp
public sealed record UpgradeVerificationCommand(
    Guid IdentityId,
    VerificationLevel Level);
```

---

# EVENTS

Create:

```
events/IdentityAttributeAddedEvent.cs
```

```csharp
public sealed record IdentityAttributeAddedEvent(
    Guid IdentityId,
    string Key,
    string Value,
    DateTime CreatedAt);
```

---

Create:

```
events/IdentityVerificationUpgradedEvent.cs
```

```csharp
public sealed record IdentityVerificationUpgradedEvent(
    Guid IdentityId,
    VerificationLevel Level,
    DateTime VerifiedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# ATTRIBUTE ENGINE

Create:

```
IdentityAttributeEngine.cs
```

```csharp
public sealed class IdentityAttributeEngine
{
    public IdentityAttributeAddedEvent Execute(
        AddIdentityAttributeCommand command,
        IdentityAttributeRegistry registry)
    {
        var attribute = new IdentityAttribute(
            command.IdentityId,
            command.Key,
            command.Value);

        registry.AddAttribute(attribute);

        return new IdentityAttributeAddedEvent(
            command.IdentityId,
            command.Key,
            command.Value,
            attribute.CreatedAt);
    }
}
```

---

# VERIFICATION ENGINE

Create:

```
IdentityVerificationUpgradeEngine.cs
```

```csharp
public sealed class IdentityVerificationUpgradeEngine
{
    public IdentityVerificationUpgradedEvent Execute(
        UpgradeVerificationCommand command,
        IdentityVerificationRegistry registry)
    {
        var record = registry.GetOrCreate(command.IdentityId);

        record.Upgrade(command.Level);

        return new IdentityVerificationUpgradedEvent(
            command.IdentityId,
            command.Level,
            record.VerifiedAt);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.IdentityVerification.Tests/
```

Tests:

```
IdentityAttributeTests
VerificationUpgradeTests
VerificationRegistryTests
```

Example:

```csharp
[Fact]
public void VerificationUpgrade_ShouldIncreaseLevel()
{
    var registry = new IdentityVerificationRegistry();

    var engine = new IdentityVerificationUpgradeEngine();

    var cmd = new UpgradeVerificationCommand(
        Guid.NewGuid(),
        VerificationLevel.Basic);

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

Expected:

```
Build succeeded
0 errors
0 warnings
```

---

# SUCCESS CRITERIA

Attributes can be stored  
Verification levels upgrade correctly  
Verification records persist in registry  
Events emitted correctly  
Unit tests pass  

---

# END OF PHASE 2.0.2