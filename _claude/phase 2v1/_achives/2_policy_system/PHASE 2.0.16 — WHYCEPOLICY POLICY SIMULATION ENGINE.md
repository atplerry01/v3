# WHYCESPACE WBSM v3

# PHASE 2.0.16 — WHYCEPOLICY POLICY SIMULATION ENGINE
(Engine Architecture)

You are implementing the **Policy Simulation Engine**.

This engine simulates the effect of policies using **sample contexts**.

The simulation engine allows governance to test policies **before activation**.

Example:

Policy:

```
policy SPV_CAPITAL_LIMIT
when spv.capital > 100000000
then deny("SPV capital exceeds limit")
```

Simulation contexts:

```
spv.capital = 50M
spv.capital = 120M
spv.capital = 200M
```

Simulation results:

```
ALLOW
DENY
DENY
```

Follow WBSM rules:

ENGINE → deterministic simulation logic  
SYSTEM → policy state  

---

# OBJECTIVES

Implement:

ENGINE COMPONENTS

• PolicySimulationContext  
• PolicySimulationResult  
• PolicySimulationEngine  

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
simulation/
```

Structure:

```
simulation/

├── PolicySimulationContext.cs
├── PolicySimulationResult.cs
└── PolicySimulationEngine.cs
```

---

# POLICY SIMULATION CONTEXT

Create:

```
simulation/PolicySimulationContext.cs
```

This represents **one simulation input scenario**.

```csharp
public sealed class PolicySimulationContext
{
    public string ScenarioName { get; }

    public PolicyContext Context { get; }

    public PolicySimulationContext(
        string scenarioName,
        PolicyContext context)
    {
        ScenarioName = scenarioName;
        Context = context;
    }
}
```

---

# POLICY SIMULATION RESULT

Create:

```
simulation/PolicySimulationResult.cs
```

```csharp
public sealed class PolicySimulationResult
{
    public string ScenarioName { get; }

    public PolicyDecision Decision { get; }

    public string Message { get; }

    public PolicySimulationResult(
        string scenarioName,
        PolicyDecision decision,
        string message)
    {
        ScenarioName = scenarioName;
        Decision = decision;
        Message = message;
    }
}
```

---

# POLICY SIMULATION ENGINE

Create:

```
simulation/PolicySimulationEngine.cs
```

```csharp
public sealed class PolicySimulationEngine
{
    private readonly PolicyEvaluationEngine _evaluationEngine;

    public PolicySimulationEngine()
    {
        _evaluationEngine = new PolicyEvaluationEngine();
    }

    public IReadOnlyList<PolicySimulationResult> Simulate(
        PolicyDefinition policy,
        IReadOnlyList<PolicySimulationContext> scenarios)
    {
        var results = new List<PolicySimulationResult>();

        foreach (var scenario in scenarios)
        {
            var evaluation = _evaluationEngine.Evaluate(
                policy,
                scenario.Context);

            results.Add(
                new PolicySimulationResult(
                    scenario.ScenarioName,
                    evaluation.Decision,
                    evaluation.Message));
        }

        return results.AsReadOnly();
    }
}
```

---

# COMMAND

Create:

```
commands/SimulatePolicyCommand.cs
```

```csharp
public sealed record SimulatePolicyCommand(
    PolicyDefinition Policy,
    IReadOnlyList<PolicySimulationContext> Scenarios);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicySimulation.Tests/
```

Example:

```
PolicySimulationTests.cs
```

```csharp
[Fact]
public void PolicySimulation_ShouldReturnResults()
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

    var context1 = new PolicyContext();
    context1.Set("spv.capital", 50000000);

    var context2 = new PolicyContext();
    context2.Set("spv.capital", 150000000);

    var scenarios = new List<PolicySimulationContext>
    {
        new PolicySimulationContext("LowCapital", context1),
        new PolicySimulationContext("HighCapital", context2)
    };

    var engine = new PolicySimulationEngine();

    var results = engine.Simulate(policy, scenarios);

    Assert.Equal(2, results.Count);
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

Policies simulated correctly  
Multiple scenarios supported  
Simulation deterministic  
Simulation results returned correctly  
Unit tests pass  

---

# END OF PHASE 2.0.16