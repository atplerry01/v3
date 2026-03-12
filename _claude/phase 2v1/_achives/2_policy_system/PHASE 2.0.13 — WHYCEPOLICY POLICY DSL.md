# WHYCESPACE WBSM v3

# PHASE 2.0.13 — WHYCEPOLICY POLICY DSL
(System Architecture)

You are implementing the **Policy DSL subsystem** for WhycePolicy.

The Policy DSL defines how constitutional rules are expressed inside Whycespace.

Policies will later control:

• governance  
• capital limits  
• SPV behavior  
• identity permissions  
• vault protections  
• cluster governance  

Policies are **declarative**.

Policies do **not mutate system state**.

Policies only return **decisions**.

Follow WBSM rules:

SYSTEM → policy definitions  
ENGINE → evaluation logic  

---

# OBJECTIVES

Implement the following:

DSL COMPONENTS

• PolicyDefinition  
• PolicyCondition  
• PolicyAction  
• PolicyDecision  

PARSER COMPONENT

• PolicyParser  

Also implement:

• Commands  
• Unit tests  

---

# MODULE LOCATION

Create module:

```
src/system/upstream/WhycePolicy/
```

Structure:

```
src/system/upstream/WhycePolicy/

├── dsl/
│   ├── PolicyDefinition.cs
│   ├── PolicyCondition.cs
│   ├── PolicyAction.cs
│   └── PolicyDecision.cs
│
├── parser/
│   └── PolicyParser.cs
│
└── models/
```

Create project:

```
Whycespace.PolicySystem.csproj
```

Target framework:

```
net8.0
```

---

# POLICY DECISION ENUM

Create:

```
dsl/PolicyDecision.cs
```

```csharp
public enum PolicyDecision
{
    Allow = 1,
    Deny = 2,
    Warn = 3
}
```

---

# POLICY ACTION

Create:

```
dsl/PolicyAction.cs
```

```csharp
public sealed class PolicyAction
{
    public PolicyDecision Decision { get; }

    public string Message { get; }

    public PolicyAction(
        PolicyDecision decision,
        string message)
    {
        Decision = decision;
        Message = message;
    }
}
```

---

# POLICY CONDITION

Create:

```
dsl/PolicyCondition.cs
```

```csharp
public sealed class PolicyCondition
{
    public string Field { get; }

    public string Operator { get; }

    public string Value { get; }

    public PolicyCondition(
        string field,
        string op,
        string value)
    {
        Field = field;
        Operator = op;
        Value = value;
    }
}
```

---

# POLICY DEFINITION

Create:

```
dsl/PolicyDefinition.cs
```

```csharp
public sealed class PolicyDefinition
{
    public string PolicyId { get; }

    public string Name { get; }

    public IReadOnlyList<PolicyCondition> Conditions { get; }

    public PolicyAction Action { get; }

    public PolicyDefinition(
        string policyId,
        string name,
        IReadOnlyList<PolicyCondition> conditions,
        PolicyAction action)
    {
        PolicyId = policyId;
        Name = name;
        Conditions = conditions;
        Action = action;
    }
}
```

---

# POLICY PARSER

Create:

```
parser/PolicyParser.cs
```

This is a **simple parser** that converts DSL text into a `PolicyDefinition`.

```csharp
public sealed class PolicyParser
{
    public PolicyDefinition Parse(string policyText)
    {
        if (string.IsNullOrWhiteSpace(policyText))
            throw new ArgumentException("Policy text cannot be empty");

        var lines = policyText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToList();

        var policyId = lines[0].Replace("policy", "").Trim();

        var conditionLine = lines
            .First(l => l.StartsWith("when"));

        var actionLine = lines
            .First(l => l.StartsWith("then"));

        var conditionParts = conditionLine
            .Replace("when", "")
            .Trim()
            .Split(' ');

        var condition = new PolicyCondition(
            conditionParts[0],
            conditionParts[1],
            conditionParts[2]);

        PolicyAction action;

        if (actionLine.Contains("deny"))
        {
            var message = actionLine
                .Substring(actionLine.IndexOf("(") + 1)
                .Replace(")", "")
                .Replace("\"", "");

            action = new PolicyAction(
                PolicyDecision.Deny,
                message);
        }
        else
        {
            action = new PolicyAction(
                PolicyDecision.Allow,
                "Allowed");
        }

        return new PolicyDefinition(
            policyId,
            policyId,
            new List<PolicyCondition> { condition },
            action);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.PolicyDsl.Tests/
```

Test:

```
PolicyParserTests.cs
```

Example:

```csharp
[Fact]
public void PolicyParser_ShouldParsePolicy()
{
    var parser = new PolicyParser();

    var text = @"
policy SPV_CAPITAL_LIMIT
when spv.capital > 100000000
then deny(""Capital exceeds limit"")
";

    var policy = parser.Parse(text);

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

Policy DSL defined  
Parser converts DSL → PolicyDefinition  
Policy actions defined  
Policy conditions defined  
Unit tests pass  

---

# END OF PHASE 2.0.13