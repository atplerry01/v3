# WHYCESPACE WBSM v3

# PHASE 2.0.20 — WHYCEPOLICY GOVERNANCE AUTHORITY ENGINE
(System + Engine Architecture)

You are implementing the **Governance Authority Engine**.

This component determines **who is authorized to approve or activate policies**.

Only authorized governance bodies can approve constitutional policies.

Examples of authorities:

• GuardianCouncil
• GovernanceCommittee
• ClusterAuthority
• ConstitutionalQuorum

Policies cannot move to **Approved** state without proper authority.

Follow WBSM rules:

SYSTEM → governance authority data
ENGINE → authorization validation logic

---

# OBJECTIVES

Implement:

SYSTEM COMPONENTS

• GovernanceAuthorityType  
• GovernanceAuthority  
• GovernanceAuthorityRegistry  

ENGINE COMPONENT

• GovernanceAuthorityEngine  

Also implement:

• Commands  
• Unit tests  

---

# MODULE LOCATION

SYSTEM:

```
src/system/upstream/WhycePolicy/
```

Create folder:

```
governance/
```

Structure:

```
governance/

├── GovernanceAuthorityType.cs
├── GovernanceAuthority.cs
└── GovernanceAuthorityRegistry.cs
```

---

# GOVERNANCE AUTHORITY TYPE

Create:

```
governance/GovernanceAuthorityType.cs
```

```csharp
public enum GovernanceAuthorityType
{
    GuardianCouncil = 1,
    GovernanceCommittee = 2,
    ClusterAuthority = 3,
    ConstitutionalQuorum = 4
}
```

---

# GOVERNANCE AUTHORITY MODEL

Create:

```
governance/GovernanceAuthority.cs
```

```csharp
public sealed class GovernanceAuthority
{
    public string AuthorityId { get; }

    public GovernanceAuthorityType Type { get; }

    public IReadOnlyList<string> Members { get; }

    public GovernanceAuthority(
        string authorityId,
        GovernanceAuthorityType type,
        IReadOnlyList<string> members)
    {
        AuthorityId = authorityId;
        Type = type;
        Members = members;
    }
}
```

---

# GOVERNANCE AUTHORITY REGISTRY

Create:

```
governance/GovernanceAuthorityRegistry.cs
```

```csharp
public sealed class GovernanceAuthorityRegistry
{
    private readonly ConcurrentDictionary<string, GovernanceAuthority> _authorities
        = new();

    public void Register(GovernanceAuthority authority)
    {
        if (!_authorities.TryAdd(authority.AuthorityId, authority))
            throw new InvalidOperationException("Authority already exists");
    }

    public GovernanceAuthority Get(string authorityId)
    {
        if (!_authorities.TryGetValue(authorityId, out var authority))
            throw new KeyNotFoundException("Authority not found");

        return authority;
    }

    public IReadOnlyCollection<GovernanceAuthority> GetAll()
    {
        return _authorities.Values.ToList().AsReadOnly();
    }
}
```

---

# ENGINE LOCATION

Create engine in:

```
src/engines/T0U_Constitutional/WhycePolicy/
```

Create folder:

```
governance/
```

Structure:

```
governance/

└── GovernanceAuthorityEngine.cs
```

---

# GOVERNANCE AUTHORITY ENGINE

Create:

```
GovernanceAuthorityEngine.cs
```

```csharp
public sealed class GovernanceAuthorityEngine
{
    public bool CanApprovePolicy(
        GovernanceAuthority authority,
        string memberId)
    {
        return authority.Members.Contains(memberId);
    }
}
```

---

# COMMAND

Create:

```
commands/ApprovePolicyCommand.cs
```

```csharp
public sealed record ApprovePolicyCommand(
    string PolicyId,
    string AuthorityId,
    string MemberId);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyGovernance.Tests/
```

Example:

```
GovernanceAuthorityTests.cs
```

```csharp
[Fact]
public void Authority_ShouldApprovePolicy()
{
    var authority = new GovernanceAuthority(
        "GuardianCouncil",
        GovernanceAuthorityType.GuardianCouncil,
        new List<string> { "guardian1", "guardian2" });

    var engine = new GovernanceAuthorityEngine();

    var result = engine.CanApprovePolicy(
        authority,
        "guardian1");

    Assert.True(result);
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

Authorities registered correctly  
Authority membership validated  
Policy approval permissions verified  
Unit tests pass  

---

# END OF PHASE 2.0.20