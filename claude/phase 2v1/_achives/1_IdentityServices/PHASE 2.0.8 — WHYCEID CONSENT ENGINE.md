# WHYCESPACE WBSM v3

# PHASE 2.0.8 — WHYCEID CONSENT ENGINE
(System + Engine Architecture)

You are implementing the **Consent subsystem of WhyceID**.

Consent management allows identities to grant or revoke permissions for specific activities within Whycespace.

Examples:

• data processing consent  
• cluster participation consent  
• investment authorization  
• delegated authority  
• device trust consent  

Consent is required for **regulatory compliance** and **governance transparency**.

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• IdentityConsent  
• ConsentRecord  
• ConsentRegistry  

ENGINE COMPONENT

• GrantConsentEngine  
• RevokeConsentEngine  
• ConsentCheckEngine  

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
consent/
```

Structure:

```
consent/

├── IdentityConsent.cs
├── ConsentRecord.cs
└── ConsentRegistry.cs
```

---

# CONSENT TYPE ENUM

Create:

```
consent/ConsentType.cs
```

```csharp
public enum ConsentType
{
    DataProcessing = 1,
    ClusterParticipation = 2,
    InvestmentAuthorization = 3,
    DeviceTrust = 4,
    DelegatedAuthority = 5
}
```

---

# IDENTITY CONSENT MODEL

Create:

```
consent/IdentityConsent.cs
```

```csharp
public sealed class IdentityConsent
{
    public ConsentType Type { get; }

    public string Description { get; }

    public IdentityConsent(ConsentType type, string description)
    {
        Type = type;
        Description = description;
    }
}
```

---

# CONSENT RECORD

Create:

```
consent/ConsentRecord.cs
```

```csharp
public sealed class ConsentRecord
{
    public Guid IdentityId { get; }

    public ConsentType Type { get; }

    public DateTime GrantedAt { get; }

    public bool Active { get; private set; }

    public ConsentRecord(Guid identityId, ConsentType type)
    {
        IdentityId = identityId;
        Type = type;
        GrantedAt = DateTime.UtcNow;
        Active = true;
    }

    public void Revoke()
    {
        Active = false;
    }
}
```

---

# CONSENT REGISTRY

Create:

```
consent/ConsentRegistry.cs
```

```csharp
public sealed class ConsentRegistry
{
    private readonly ConcurrentDictionary<(Guid, ConsentType), ConsentRecord> _records
        = new();

    public void Grant(ConsentRecord record)
    {
        var key = (record.IdentityId, record.Type);

        if (!_records.TryAdd(key, record))
            throw new InvalidOperationException("Consent already exists");
    }

    public ConsentRecord Get(Guid identityId, ConsentType type)
    {
        if (!_records.TryGetValue((identityId, type), out var record))
            throw new KeyNotFoundException("Consent not found");

        return record;
    }

    public bool HasActiveConsent(Guid identityId, ConsentType type)
    {
        if (!_records.TryGetValue((identityId, type), out var record))
            return false;

        return record.Active;
    }
}
```

---

# COMMANDS

Create:

```
commands/GrantConsentCommand.cs
```

```csharp
public sealed record GrantConsentCommand(
    Guid IdentityId,
    ConsentType Type);
```

---

Create:

```
commands/RevokeConsentCommand.cs
```

```csharp
public sealed record RevokeConsentCommand(
    Guid IdentityId,
    ConsentType Type);
```

---

Create:

```
commands/CheckConsentCommand.cs
```

```csharp
public sealed record CheckConsentCommand(
    Guid IdentityId,
    ConsentType Type);
```

---

# EVENTS

Create:

```
events/ConsentGrantedEvent.cs
```

```csharp
public sealed record ConsentGrantedEvent(
    Guid IdentityId,
    ConsentType Type,
    DateTime GrantedAt);
```

---

Create:

```
events/ConsentRevokedEvent.cs
```

```csharp
public sealed record ConsentRevokedEvent(
    Guid IdentityId,
    ConsentType Type,
    DateTime RevokedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# GRANT CONSENT ENGINE

Create:

```
GrantConsentEngine.cs
```

```csharp
public sealed class GrantConsentEngine
{
    public ConsentGrantedEvent Execute(
        GrantConsentCommand command,
        ConsentRegistry registry)
    {
        var record = new ConsentRecord(
            command.IdentityId,
            command.Type);

        registry.Grant(record);

        return new ConsentGrantedEvent(
            command.IdentityId,
            command.Type,
            record.GrantedAt);
    }
}
```

---

# REVOKE CONSENT ENGINE

Create:

```
RevokeConsentEngine.cs
```

```csharp
public sealed class RevokeConsentEngine
{
    public ConsentRevokedEvent Execute(
        RevokeConsentCommand command,
        ConsentRegistry registry)
    {
        var record = registry.Get(
            command.IdentityId,
            command.Type);

        record.Revoke();

        return new ConsentRevokedEvent(
            command.IdentityId,
            command.Type,
            DateTime.UtcNow);
    }
}
```

---

# CONSENT CHECK ENGINE

Create:

```
ConsentCheckEngine.cs
```

```csharp
public sealed class ConsentCheckEngine
{
    public bool Execute(
        CheckConsentCommand command,
        ConsentRegistry registry)
    {
        return registry.HasActiveConsent(
            command.IdentityId,
            command.Type);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.Consent.Tests/
```

Tests:

```
ConsentGrantTests
ConsentRevokeTests
ConsentRegistryTests
```

Example:

```csharp
[Fact]
public void Consent_ShouldBeGranted()
{
    var registry = new ConsentRegistry();

    var engine = new GrantConsentEngine();

    var id = Guid.NewGuid();

    var result = engine.Execute(
        new GrantConsentCommand(id, ConsentType.DataProcessing),
        registry);

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

Consent granted correctly  
Consent revoked correctly  
Consent checks work correctly  
Duplicate consents prevented  
Unit tests pass  

---

# END OF PHASE 2.0.8