# WHYCESPACE WBSM v3

# PHASE 2.0.23 — WHYCEPOLICY POLICY ENFORCEMENT MIDDLEWARE
(Runtime Architecture)

You are implementing the **Policy Enforcement Middleware**.

This middleware intercepts **runtime requests** and enforces policies before engine execution.

Every command execution must pass through policy enforcement.

Flow:

```
Request
   ↓
Policy Enforcement Middleware
   ↓
Policy Evaluation
   ↓
Allow / Deny
   ↓
Engine Execution
```

If a policy decision is **DENY**, the request must be blocked.

Follow WBSM rules:

SYSTEM → policy state  
ENGINE → evaluation logic  
MIDDLEWARE → runtime enforcement  

---

# OBJECTIVES

Implement:

RUNTIME COMPONENTS

• PolicyEnforcementRequest  
• PolicyEnforcementResult  
• PolicyEnforcementMiddleware  

Also implement:

• Commands  
• Unit tests  

---

# MODULE LOCATION

Create runtime module:

```
src/runtime/policy/
```

Structure:

```
src/runtime/policy/

├── PolicyEnforcementRequest.cs
├── PolicyEnforcementResult.cs
└── PolicyEnforcementMiddleware.cs
```

Create project:

```
Whycespace.PolicyRuntime.csproj
```

References:

```
Whycespace.PolicySystem
Whycespace.PolicyEngines
```

---

# POLICY ENFORCEMENT REQUEST

Create:

```
PolicyEnforcementRequest.cs
```

```csharp
public sealed class PolicyEnforcementRequest
{
    public string PolicyId { get; }

    public PolicyContext Context { get; }

    public PolicyEnforcementRequest(
        string policyId,
        PolicyContext context)
    {
        PolicyId = policyId;
        Context = context;
    }
}
```

---

# POLICY ENFORCEMENT RESULT

Create:

```
PolicyEnforcementResult.cs
```

```csharp
public sealed class PolicyEnforcementResult
{
    public bool Allowed { get; }

    public string Message { get; }

    public PolicyEnforcementResult(
        bool allowed,
        string message)
    {
        Allowed = allowed;
        Message = message;
    }
}
```

---

# POLICY ENFORCEMENT MIDDLEWARE

Create:

```
PolicyEnforcementMiddleware.cs
```

```csharp
public sealed class PolicyEnforcementMiddleware
{
    private readonly PolicyRegistry _registry;

    private readonly PolicyEvaluationEngine _evaluationEngine;

    public PolicyEnforcementMiddleware(
        PolicyRegistry registry)
    {
        _registry = registry;
        _evaluationEngine = new PolicyEvaluationEngine();
    }

    public PolicyEnforcementResult Enforce(
        PolicyEnforcementRequest request)
    {
        var version = _registry.GetActive(request.PolicyId);

        var result = _evaluationEngine.Evaluate(
            version.Definition,
            request.Context);

        if (result.Decision == PolicyDecision.Deny)
        {
            return new PolicyEnforcementResult(
                false,
                result.Message);
        }

        return new PolicyEnforcementResult(
            true,
            result.Message);
    }
}
```

---

# COMMAND

Create:

```
commands/EnforcePolicyCommand.cs
```

```csharp
public sealed record EnforcePolicyCommand(
    string PolicyId,
    PolicyContext Context);
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyRuntime.Tests/
```

Example:

```
PolicyEnforcementTests.cs
```

```csharp
[Fact]
public void Middleware_ShouldBlockRequest_WhenPolicyDenies()
{
    var registry = new PolicyRegistry();

    registry.RegisterPolicy("SPV_CAPITAL_LIMIT");

    var definition = new PolicyDefinition(
        "SPV_CAPITAL_LIMIT",
        "SPV_CAPITAL_LIMIT",
        new List<PolicyCondition>
        {
            new PolicyCondition("spv.capital", ">", "100")
        },
        new PolicyAction(
            PolicyDecision.Deny,
            "Capital exceeded"));

    var version = new PolicyVersion("v1", definition);

    registry.AddVersion("SPV_CAPITAL_LIMIT", version);

    version.Activate();

    var middleware = new PolicyEnforcementMiddleware(registry);

    var context = new PolicyContext();

    context.Set("spv.capital", 200);

    var result = middleware.Enforce(
        new PolicyEnforcementRequest(
            "SPV_CAPITAL_LIMIT",
            context));

    Assert.False(result.Allowed);
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

Policy enforcement intercepts requests  
Policies evaluated before execution  
Denied requests blocked  
Allowed requests proceed  
Unit tests pass  

---

# END OF PHASE 2.0.23