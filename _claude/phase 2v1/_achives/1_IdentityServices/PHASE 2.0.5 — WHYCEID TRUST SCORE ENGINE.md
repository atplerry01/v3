# WHYCESPACE WBSM v3

# PHASE 2.0.5 — WHYCEID TRUST SCORE ENGINE
(System + Engine Architecture)

You are implementing the **TrustScore subsystem of WhyceID**.

TrustScore represents the **reputation and trustworthiness of an identity** within Whycespace.

TrustScore will be used across the ecosystem for:

• governance eligibility  
• CWG participation  
• capital access  
• SPV participation  
• operator privileges  

TrustScore evolves through **events and behavioral updates**.

Example influences:

```
positive:
+ verified identity
+ governance participation
+ successful SPV participation

negative:
- fraud detection
- governance violations
- system abuse
```

Follow WBSM architecture rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• TrustScoreRecord  
• TrustScoreRegistry  

ENGINE COMPONENT

• TrustScoreIncreaseEngine  
• TrustScoreDecreaseEngine  

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
trustscore/
```

Structure:

```
trustscore/

├── TrustScoreRecord.cs
└── TrustScoreRegistry.cs
```

---

# TRUST SCORE RECORD

Create:

```
trustscore/TrustScoreRecord.cs
```

```csharp
public sealed class TrustScoreRecord
{
    public Guid IdentityId { get; }

    public int Score { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public TrustScoreRecord(Guid identityId)
    {
        IdentityId = identityId;
        Score = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Increase(int amount)
    {
        Score += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Decrease(int amount)
    {
        Score -= amount;

        if (Score < 0)
            Score = 0;

        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

# TRUST SCORE REGISTRY

Create:

```
trustscore/TrustScoreRegistry.cs
```

```csharp
public sealed class TrustScoreRegistry
{
    private readonly ConcurrentDictionary<Guid, TrustScoreRecord> _records
        = new();

    public TrustScoreRecord GetOrCreate(Guid identityId)
    {
        return _records.GetOrAdd(identityId, id => new TrustScoreRecord(id));
    }

    public TrustScoreRecord Get(Guid identityId)
    {
        if (!_records.TryGetValue(identityId, out var record))
            throw new KeyNotFoundException("TrustScore record not found");

        return record;
    }
}
```

---

# COMMANDS

Create:

```
commands/IncreaseTrustScoreCommand.cs
```

```csharp
public sealed record IncreaseTrustScoreCommand(
    Guid IdentityId,
    int Amount,
    string Reason);
```

---

Create:

```
commands/DecreaseTrustScoreCommand.cs
```

```csharp
public sealed record DecreaseTrustScoreCommand(
    Guid IdentityId,
    int Amount,
    string Reason);
```

---

# EVENTS

Create:

```
events/TrustScoreIncreasedEvent.cs
```

```csharp
public sealed record TrustScoreIncreasedEvent(
    Guid IdentityId,
    int Amount,
    int NewScore,
    string Reason,
    DateTime UpdatedAt);
```

---

Create:

```
events/TrustScoreDecreasedEvent.cs
```

```csharp
public sealed record TrustScoreDecreasedEvent(
    Guid IdentityId,
    int Amount,
    int NewScore,
    string Reason,
    DateTime UpdatedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# TRUST SCORE INCREASE ENGINE

Create:

```
TrustScoreIncreaseEngine.cs
```

```csharp
public sealed class TrustScoreIncreaseEngine
{
    public TrustScoreIncreasedEvent Execute(
        IncreaseTrustScoreCommand command,
        TrustScoreRegistry registry)
    {
        var record = registry.GetOrCreate(command.IdentityId);

        record.Increase(command.Amount);

        return new TrustScoreIncreasedEvent(
            command.IdentityId,
            command.Amount,
            record.Score,
            command.Reason,
            record.UpdatedAt);
    }
}
```

---

# TRUST SCORE DECREASE ENGINE

Create:

```
TrustScoreDecreaseEngine.cs
```

```csharp
public sealed class TrustScoreDecreaseEngine
{
    public TrustScoreDecreasedEvent Execute(
        DecreaseTrustScoreCommand command,
        TrustScoreRegistry registry)
    {
        var record = registry.GetOrCreate(command.IdentityId);

        record.Decrease(command.Amount);

        return new TrustScoreDecreasedEvent(
            command.IdentityId,
            command.Amount,
            record.Score,
            command.Reason,
            record.UpdatedAt);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.TrustScore.Tests/
```

Tests:

```
TrustScoreIncreaseTests
TrustScoreDecreaseTests
TrustScoreRegistryTests
```

Example:

```csharp
[Fact]
public void TrustScore_ShouldIncrease()
{
    var registry = new TrustScoreRegistry();

    var engine = new TrustScoreIncreaseEngine();

    var id = Guid.NewGuid();

    var result = engine.Execute(
        new IncreaseTrustScoreCommand(id, 10, "Verified identity"),
        registry);

    Assert.Equal(10, result.NewScore);
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

Trust score records created  
Score increases correctly  
Score decreases correctly  
Score never goes negative  
Events emitted correctly  
Unit tests pass  

---

# END OF PHASE 2.0.5