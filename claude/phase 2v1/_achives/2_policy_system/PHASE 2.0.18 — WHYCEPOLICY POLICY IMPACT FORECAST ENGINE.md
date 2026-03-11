# WHYCESPACE WBSM v3

# PHASE 2.0.18 — WHYCEPOLICY POLICY IMPACT FORECAST ENGINE
(Engine Architecture)

You are implementing the **Policy Impact Forecast Engine**.

This engine forecasts the **potential impact of a policy before activation**.

The goal is to help governance understand **how a policy will affect the system**.

Example forecasts:

• number of SPVs affected  
• number of identities impacted  
• number of transactions blocked  
• number of vault operations restricted  

The forecast engine does **not mutate system state**.

It only produces **impact predictions**.

Follow WBSM rules:

ENGINE → deterministic forecasting logic  
SYSTEM → policy definitions  

---

# OBJECTIVES

Implement:

ENGINE COMPONENTS

• PolicyImpactScenario  
• PolicyImpactResult  
• PolicyImpactForecastEngine  

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
forecast/
```

Structure:

```
forecast/

├── PolicyImpactScenario.cs
├── PolicyImpactResult.cs
└── PolicyImpactForecastEngine.cs
```

---

# IMPACT SCENARIO

Create:

```
forecast/PolicyImpactScenario.cs
```

A scenario represents **system statistics used during forecasting**.

```csharp
public sealed class PolicyImpactScenario
{
    public int SpvCount { get; }

    public int IdentityCount { get; }

    public int TransactionVolume { get; }

    public PolicyImpactScenario(
        int spvCount,
        int identityCount,
        int transactionVolume)
    {
        SpvCount = spvCount;
        IdentityCount = identityCount;
        TransactionVolume = transactionVolume;
    }
}
```

---

# IMPACT RESULT

Create:

```
forecast/PolicyImpactResult.cs
```

```csharp
public sealed class PolicyImpactResult
{
    public string PolicyId { get; }

    public int AffectedSpvs { get; }

    public int AffectedIdentities { get; }

    public int BlockedTransactions { get; }

    public PolicyImpactResult(
        string policyId,
        int affectedSpvs,
        int affectedIdentities,
        int blockedTransactions)
    {
        PolicyId = policyId;
        AffectedSpvs = affectedSpvs;
        AffectedIdentities = affectedIdentities;
        BlockedTransactions = blockedTransactions;
    }
}
```

---

# IMPACT FORECAST ENGINE

Create:

```
forecast/PolicyImpactForecastEngine.cs
```

```csharp
public sealed class PolicyImpactForecastEngine
{
    public PolicyImpactResult Forecast(
        PolicyDefinition policy,
        PolicyImpactScenario scenario)
    {
        var affectedSpvs = scenario.SpvCount / 10;
        var affectedIdentities = scenario.IdentityCount / 20;
        var blockedTransactions = scenario.TransactionVolume / 15;

        if (policy.Action.Decision == PolicyDecision.Allow)
        {
            affectedSpvs = 0;
            affectedIdentities = 0;
            blockedTransactions = 0;
        }

        return new PolicyImpactResult(
            policy.PolicyId,
            affectedSpvs,
            affectedIdentities,
            blockedTransactions);
    }
}
```

---

# COMMAND

Create:

```
commands/ForecastPolicyImpactCommand.cs
```

```csharp
public sealed record ForecastPolicyImpactCommand(
    PolicyDefinition Policy,
    PolicyImpactScenario Scenario);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyForecast.Tests/
```

Example:

```
PolicyImpactForecastTests.cs
```

```csharp
[Fact]
public void ForecastEngine_ShouldReturnImpact()
{
    var policy = new PolicyDefinition(
        "SPV_CAPITAL_LIMIT",
        "SPV_CAPITAL_LIMIT",
        new List<PolicyCondition>
        {
            new PolicyCondition("spv.capital", ">", "100000000")
        },
        new PolicyAction(
            PolicyDecision.Deny,
            "Capital exceeds limit")
    );

    var scenario = new PolicyImpactScenario(
        100,
        1000,
        50000);

    var engine = new PolicyImpactForecastEngine();

    var result = engine.Forecast(policy, scenario);

    Assert.Equal("SPV_CAPITAL_LIMIT", result.PolicyId);
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

Policy impact forecast generated  
System metrics processed  
Forecast deterministic  
Unit tests pass  

---

# END OF PHASE 2.0.18