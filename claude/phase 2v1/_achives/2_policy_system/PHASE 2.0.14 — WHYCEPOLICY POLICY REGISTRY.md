# WHYCESPACE WBSM v3

# PHASE 2.0.14 — WHYCEPOLICY POLICY REGISTRY
(System Architecture)

You are implementing the **Policy Registry subsystem** for WhycePolicy.

The Policy Registry is the **central repository of all policies** in Whycespace.

The registry manages:

• policy storage  
• policy versioning  
• active policy selection  
• policy lookup  

Policies must be immutable once registered.

New versions must be registered separately.

Follow WBSM rules:

SYSTEM → policy state and registries  
ENGINE → deterministic logic  

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• PolicyRecord  
• PolicyVersion  
• PolicyRegistry  

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
registry/
```

Structure:

```
registry/

├── PolicyRecord.cs
├── PolicyVersion.cs
└── PolicyRegistry.cs
```

---

# POLICY VERSION

Create:

```
registry/PolicyVersion.cs
```

```csharp
public sealed class PolicyVersion
{
    public string Version { get; }

    public PolicyDefinition Definition { get; }

    public DateTime CreatedAt { get; }

    public bool Active { get; private set; }

    public PolicyVersion(
        string version,
        PolicyDefinition definition)
    {
        Version = version;
        Definition = definition;
        CreatedAt = DateTime.UtcNow;
        Active = false;
    }

    public void Activate()
    {
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
    }
}
```

---

# POLICY RECORD

Create:

```
registry/PolicyRecord.cs
```

```csharp
public sealed class PolicyRecord
{
    public string PolicyId { get; }

    private readonly List<PolicyVersion> _versions = new();

    public PolicyRecord(string policyId)
    {
        PolicyId = policyId;
    }

    public void AddVersion(PolicyVersion version)
    {
        _versions.Add(version);
    }

    public IReadOnlyList<PolicyVersion> GetVersions()
    {
        return _versions.AsReadOnly();
    }

    public PolicyVersion GetActiveVersion()
    {
        var version = _versions.FirstOrDefault(v => v.Active);

        if (version == null)
            throw new InvalidOperationException("No active policy version");

        return version;
    }

    public void ActivateVersion(string version)
    {
        foreach (var v in _versions)
            v.Deactivate();

        var target = _versions
            .FirstOrDefault(v => v.Version == version);

        if (target == null)
            throw new InvalidOperationException("Version not found");

        target.Activate();
    }
}
```

---

# POLICY REGISTRY

Create:

```
registry/PolicyRegistry.cs
```

```csharp
public sealed class PolicyRegistry
{
    private readonly ConcurrentDictionary<string, PolicyRecord> _policies
        = new();

    public void RegisterPolicy(string policyId)
    {
        if (!_policies.TryAdd(policyId, new PolicyRecord(policyId)))
            throw new InvalidOperationException("Policy already exists");
    }

    public void AddVersion(
        string policyId,
        PolicyVersion version)
    {
        var record = Get(policyId);

        record.AddVersion(version);
    }

    public PolicyRecord Get(string policyId)
    {
        if (!_policies.TryGetValue(policyId, out var record))
            throw new KeyNotFoundException("Policy not found");

        return record;
    }

    public PolicyVersion GetActive(string policyId)
    {
        var record = Get(policyId);

        return record.GetActiveVersion();
    }

    public IReadOnlyCollection<PolicyRecord> GetAll()
    {
        return _policies.Values.ToList().AsReadOnly();
    }
}
```

---

# COMMANDS

Create:

```
commands/RegisterPolicyCommand.cs
```

```csharp
public sealed record RegisterPolicyCommand(
    string PolicyId);
```

---

Create:

```
commands/AddPolicyVersionCommand.cs
```

```csharp
public sealed record AddPolicyVersionCommand(
    string PolicyId,
    string Version,
    PolicyDefinition Definition);
```

---

Create:

```
commands/ActivatePolicyVersionCommand.cs
```

```csharp
public sealed record ActivatePolicyVersionCommand(
    string PolicyId,
    string Version);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyRegistry.Tests/
```

Tests:

```
PolicyRegistryTests
PolicyVersionTests
```

Example:

```csharp
[Fact]
public void PolicyRegistry_ShouldRegisterPolicy()
{
    var registry = new PolicyRegistry();

    registry.RegisterPolicy("SPV_CAPITAL_LIMIT");

    var policy = registry.Get("SPV_CAPITAL_LIMIT");

    Assert.Equal("SPV_CAPITAL_LIMIT", policy.PolicyId);
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

Policy registry stores policies  
Policy versions stored correctly  
Policy activation works  
Policy retrieval works  
Unit tests pass  

---

# END OF PHASE 2.0.14