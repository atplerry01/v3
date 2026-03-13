# WHYCESPACE WBSM v3

# PHASE 2.0.15 — WHYCEPOLICY POLICY EVALUATION ENGINE
(Engine Architecture)

You are implementing the **Policy Evaluation Engine**.

This engine evaluates a policy against a **runtime context** and produces a **policy decision**.

Example:

Policy:

```
policy SPV_CAPITAL_LIMIT
when spv.capital > 100000000
then deny("SPV capital exceeds limit")
```

Context:

```
spv.capital = 150000000
```

Evaluation result:

```
DENY
```

Follow WBSM rules:

ENGINE → deterministic logic  
SYSTEM → policy state  

---

# OBJECTIVES

Implement:

ENGINE COMPONENTS

• PolicyContext  
• PolicyEvaluationResult  
• PolicyEvaluationEngine  

Also implement:

• Commands  
• Unit tests  

---

# ENGINE LOCATION

Create module:

```
src/engines/T0U_Constitutional/WhycePolicy/
```

Create folder:

```
evaluation/
```

Structure:

```
evaluation/

├── PolicyContext.cs
├── PolicyEvaluationResult.cs
└── PolicyEvaluationEngine.cs
```

Create project:

```
Whycespace.PolicyEngines.csproj
```

References:

```
Whycespace.PolicySystem
```

---

# POLICY CONTEXT

Create:

```
evaluation/PolicyContext.cs
```

This represents **runtime data used during policy evaluation**.

```csharp
public sealed class PolicyContext
{
    private readonly Dictionary<string, object> _values
        = new();

    public void Set(string key, object value)
    {
        _values[key] = value;
    }

    public object Get(string key)
    {
        if (!_values.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Context value not found: {key}");

        return value;
    }
}
```

---

# POLICY EVALUATION RESULT

Create:

```
evaluation/PolicyEvaluationResult.cs
```

```csharp
public sealed class PolicyEvaluationResult
{
    public string PolicyId { get; }

    public PolicyDecision Decision { get; }

    public string Message { get; }

    public PolicyEvaluationResult(
        string policyId,
        PolicyDecision decision,
        string message)
    {
        PolicyId = policyId;
        Decision = decision;
        Message = message;
    }
}
```

---

# POLICY EVALUATION ENGINE

Create:

```
evaluation/PolicyEvaluationEngine.cs
```

```csharp
public sealed class PolicyEvaluationEngine
{
    public PolicyEvaluationResult Evaluate(
        PolicyDefinition policy,
        PolicyContext context)
    {
        foreach (var condition in policy.Conditions)
        {
            var value = context.Get(condition.Field);

            if (!EvaluateCondition(
                value,
                condition.Operator,
                condition.Value))
            {
                return new PolicyEvaluationResult(
                    policy.PolicyId,
                    PolicyDecision.Allow,
                    "Condition not triggered");
            }
        }

        return new PolicyEvaluationResult(
            policy.PolicyId,
            policy.Action.Decision,
            policy.Action.Message);
    }

    private bool EvaluateCondition(
        object contextValue,
        string op,
        string ruleValue)
    {
        var contextNumber = Convert.ToDecimal(contextValue);
        var ruleNumber = Convert.ToDecimal(ruleValue);

        return op switch
        {
            ">" => contextNumber > ruleNumber,
            "<" => contextNumber < ruleNumber,
            ">=" => contextNumber >= ruleNumber,
            "<=" => contextNumber <= ruleNumber,
            "==" => contextNumber == ruleNumber,
            _ => throw new InvalidOperationException("Unsupported operator")
        };
    }
}
```

---

# COMMAND

Create:

```
commands/EvaluatePolicyCommand.cs
```

```csharp
public sealed record EvaluatePolicyCommand(
    PolicyDefinition Policy,
    PolicyContext Context);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyEvaluation.Tests/
```

Example:

```
PolicyEvaluationTests.cs
```

```csharp
[Fact]
public void PolicyEvaluation_ShouldDeny_WhenConditionMet()
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

    var context = new PolicyContext();

    context.Set("spv.capital", 150000000);

    var engine = new PolicyEvaluationEngine();

    var result = engine.Evaluate(policy, context);

    Assert.Equal(PolicyDecision.Deny, result.Decision);
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

Policies evaluated correctly  
Conditions processed correctly  
Decisions returned deterministically  
Evaluation engine stateless  
Unit tests pass  

---

# END OF PHASE 2.0.15