# WHYCESPACE WBSM v3

# PHASE 2.0.17 — WHYCEPOLICY POLICY CONFLICT DETECTION ENGINE
(Engine Architecture)

You are implementing the **Policy Conflict Detection Engine**.

This engine analyzes policies to detect **logical conflicts**.

Conflicts occur when:

• two policies evaluate the same condition  
• but produce opposite decisions  

Example conflict:

Policy A:

```
when spv.capital > 100000000
then allow
```

Policy B:

```
when spv.capital > 100000000
then deny
```

The engine must detect this and report it.

Follow WBSM rules:

ENGINE → deterministic validation logic  
SYSTEM → policy definitions  

---

# OBJECTIVES

Implement:

ENGINE COMPONENTS

• PolicyConflict  
• PolicyConflictResult  
• PolicyConflictDetectionEngine  

Also implement:

• Commands  
• Unit tests  

---

# ENGINE LOCATION

Extend module:

```
src/engines/T0U_Constitutional/WhycePolicy/
```

Create folder:

```
conflict/
```

Structure:

```
conflict/

├── PolicyConflict.cs
├── PolicyConflictResult.cs
└── PolicyConflictDetectionEngine.cs
```

---

# POLICY CONFLICT MODEL

Create:

```
conflict/PolicyConflict.cs
```

```csharp
public sealed class PolicyConflict
{
    public string PolicyA { get; }

    public string PolicyB { get; }

    public string Reason { get; }

    public PolicyConflict(
        string policyA,
        string policyB,
        string reason)
    {
        PolicyA = policyA;
        PolicyB = policyB;
        Reason = reason;
    }
}
```

---

# CONFLICT RESULT

Create:

```
conflict/PolicyConflictResult.cs
```

```csharp
public sealed class PolicyConflictResult
{
    public bool HasConflict { get; }

    public IReadOnlyList<PolicyConflict> Conflicts { get; }

    public PolicyConflictResult(
        bool hasConflict,
        IReadOnlyList<PolicyConflict> conflicts)
    {
        HasConflict = hasConflict;
        Conflicts = conflicts;
    }
}
```

---

# CONFLICT DETECTION ENGINE

Create:

```
conflict/PolicyConflictDetectionEngine.cs
```

```csharp
public sealed class PolicyConflictDetectionEngine
{
    public PolicyConflictResult Detect(
        IReadOnlyList<PolicyDefinition> policies)
    {
        var conflicts = new List<PolicyConflict>();

        for (int i = 0; i < policies.Count; i++)
        {
            for (int j = i + 1; j < policies.Count; j++)
            {
                var a = policies[i];
                var b = policies[j];

                if (IsConflict(a, b))
                {
                    conflicts.Add(
                        new PolicyConflict(
                            a.PolicyId,
                            b.PolicyId,
                            "Opposite decisions for same condition"));
                }
            }
        }

        return new PolicyConflictResult(
            conflicts.Count > 0,
            conflicts.AsReadOnly());
    }

    private bool IsConflict(
        PolicyDefinition a,
        PolicyDefinition b)
    {
        if (a.Conditions.Count != b.Conditions.Count)
            return false;

        for (int i = 0; i < a.Conditions.Count; i++)
        {
            var condA = a.Conditions[i];
            var condB = b.Conditions[i];

            if (condA.Field != condB.Field ||
                condA.Operator != condB.Operator ||
                condA.Value != condB.Value)
            {
                return false;
            }
        }

        return a.Action.Decision != b.Action.Decision;
    }
}
```

---

# COMMAND

Create:

```
commands/DetectPolicyConflictCommand.cs
```

```csharp
public sealed record DetectPolicyConflictCommand(
    IReadOnlyList<PolicyDefinition> Policies);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyConflict.Tests/
```

Example:

```
PolicyConflictDetectionTests.cs
```

```csharp
[Fact]
public void ConflictEngine_ShouldDetectConflict()
{
    var policyA = new PolicyDefinition(
        "A",
        "PolicyA",
        new List<PolicyCondition>
        {
            new PolicyCondition("spv.capital", ">", "100")
        },
        new PolicyAction(
            PolicyDecision.Allow,
            "Allowed"));

    var policyB = new PolicyDefinition(
        "B",
        "PolicyB",
        new List<PolicyCondition>
        {
            new PolicyCondition("spv.capital", ">", "100")
        },
        new PolicyAction(
            PolicyDecision.Deny,
            "Denied"));

    var engine = new PolicyConflictDetectionEngine();

    var result = engine.Detect(
        new List<PolicyDefinition>
        {
            policyA,
            policyB
        });

    Assert.True(result.HasConflict);
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

Conflicts detected correctly  
Policy contradictions identified  
Conflict results returned correctly  
Unit tests pass  

---

# END OF PHASE 2.0.17