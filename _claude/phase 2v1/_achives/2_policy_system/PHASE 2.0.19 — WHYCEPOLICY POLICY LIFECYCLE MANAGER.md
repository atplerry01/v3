# WHYCESPACE WBSM v3

# PHASE 2.0.19 — WHYCEPOLICY POLICY LIFECYCLE MANAGER
(System Architecture)

You are implementing the **Policy Lifecycle Manager**.

This component manages the **governance lifecycle of policies**.

Policies must pass through lifecycle stages before activation.

Lifecycle states:

• Draft  
• Review  
• Approved  
• Active  
• Deprecated  

Policies cannot skip lifecycle stages.

Example flow:

```
Draft → Review → Approved → Active
```

Follow WBSM rules:

SYSTEM → lifecycle state  
ENGINE → lifecycle transitions  

---

# OBJECTIVES

Implement:

SYSTEM COMPONENTS

• PolicyLifecycleState  
• PolicyLifecycleRecord  
• PolicyLifecycleRegistry  

ENGINE COMPONENT

• PolicyLifecycleManager  

Also implement:

• Commands  
• Unit tests  

---

# MODULE LOCATION

Extend module:

```
src/system/upstream/WhycePolicy/
```

Create folder:

```
lifecycle/
```

Structure:

```
lifecycle/

├── PolicyLifecycleState.cs
├── PolicyLifecycleRecord.cs
├── PolicyLifecycleRegistry.cs
└── PolicyLifecycleManager.cs
```

---

# POLICY LIFECYCLE STATE

Create:

```
lifecycle/PolicyLifecycleState.cs
```

```csharp
public enum PolicyLifecycleState
{
    Draft = 1,
    Review = 2,
    Approved = 3,
    Active = 4,
    Deprecated = 5
}
```

---

# POLICY LIFECYCLE RECORD

Create:

```
lifecycle/PolicyLifecycleRecord.cs
```

```csharp
public sealed class PolicyLifecycleRecord
{
    public string PolicyId { get; }

    public PolicyLifecycleState State { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public PolicyLifecycleRecord(string policyId)
    {
        PolicyId = policyId;
        State = PolicyLifecycleState.Draft;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetState(PolicyLifecycleState state)
    {
        State = state;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

# POLICY LIFECYCLE REGISTRY

Create:

```
lifecycle/PolicyLifecycleRegistry.cs
```

```csharp
public sealed class PolicyLifecycleRegistry
{
    private readonly ConcurrentDictionary<string, PolicyLifecycleRecord> _records
        = new();

    public PolicyLifecycleRecord GetOrCreate(string policyId)
    {
        return _records.GetOrAdd(
            policyId,
            id => new PolicyLifecycleRecord(id));
    }

    public PolicyLifecycleRecord Get(string policyId)
    {
        if (!_records.TryGetValue(policyId, out var record))
            throw new KeyNotFoundException("Policy lifecycle not found");

        return record;
    }
}
```

---

# POLICY LIFECYCLE MANAGER

Create:

```
lifecycle/PolicyLifecycleManager.cs
```

```csharp
public sealed class PolicyLifecycleManager
{
    public void Transition(
        PolicyLifecycleRecord record,
        PolicyLifecycleState newState)
    {
        var current = record.State;

        if (!IsValidTransition(current, newState))
            throw new InvalidOperationException(
                $"Invalid lifecycle transition: {current} → {newState}");

        record.SetState(newState);
    }

    private bool IsValidTransition(
        PolicyLifecycleState current,
        PolicyLifecycleState next)
    {
        return (current, next) switch
        {
            (PolicyLifecycleState.Draft, PolicyLifecycleState.Review) => true,
            (PolicyLifecycleState.Review, PolicyLifecycleState.Approved) => true,
            (PolicyLifecycleState.Approved, PolicyLifecycleState.Active) => true,
            (PolicyLifecycleState.Active, PolicyLifecycleState.Deprecated) => true,
            _ => false
        };
    }
}
```

---

# COMMANDS

Create:

```
commands/TransitionPolicyLifecycleCommand.cs
```

```csharp
public sealed record TransitionPolicyLifecycleCommand(
    string PolicyId,
    PolicyLifecycleState NewState);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyLifecycle.Tests/
```

Example:

```
PolicyLifecycleTests.cs
```

```csharp
[Fact]
public void Lifecycle_ShouldTransitionCorrectly()
{
    var registry = new PolicyLifecycleRegistry();

    var manager = new PolicyLifecycleManager();

    var record = registry.GetOrCreate("SPV_CAPITAL_LIMIT");

    manager.Transition(record, PolicyLifecycleState.Review);

    Assert.Equal(
        PolicyLifecycleState.Review,
        record.State);
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

Lifecycle states stored correctly  
Lifecycle transitions validated  
Invalid transitions rejected  
Unit tests pass  

---

# END OF PHASE 2.0.19