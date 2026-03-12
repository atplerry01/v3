# WHYCESPACE WBSM v3
# PHASE 0.5 — ARCHITECTURE GUARDRAILS

You are implementing **Phase 0.5 of the Whycespace system**.

This phase establishes **Architecture Guardrails** for the entire codebase.

These guardrails enforce the **Whycespace Build Strict Mode (WBSM v3)** rules.

No code should violate these rules.

The guardrails must be enforced through:

• architecture rule validators  
• engine design contracts  
• event sourcing enforcement  
• deterministic output requirements  

---

# OBJECTIVES

1 Define architecture rules  
2 Implement architecture validator  
3 Implement engine contract validator  
4 Implement event contract validator  
5 Provide deterministic build validation  
6 Implement guardrail unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/guardrails/
```

Structure:

```
src/runtime/guardrails/
├── architecture/
├── validation/
├── rules/
├── contracts/
└── enforcement/
```

Create project:

```
Whycespace.ArchitectureGuardrails.csproj
```

Target framework:

```
net8.0
```

Guardrails belong to **runtime infrastructure**, not system.

---

# ARCHITECTURE RULES

Create:

```
ArchitectureRules.cs
```

Rules must include:

1 Engines must be stateless  
2 No engine-to-engine direct calls  
3 Engines only communicate through workflows  
4 All state mutations emit events  
5 Event sourcing is mandatory  
6 Projections must not mutate state  
7 Decision engines must read projections only  
8 Runtime dispatcher is the only execution entrypoint  

Example structure:

```csharp
public static class ArchitectureRules
{
    public const string StatelessEngines =
        "Engines must be stateless.";

    public const string NoEngineToEngineCalls =
        "Engines must not call other engines directly.";

    public const string EventSourcingRequired =
        "All state changes must emit events.";
}
```

---

# ENGINE CONTRACT

Create:

```
IEngineContract.cs
```

Example:

```csharp
public interface IEngineContract
{
    string EngineName { get; }

    Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken);
}
```

All engines must implement this interface.

---

# ENGINE VALIDATOR

Create:

```
EngineArchitectureValidator.cs
```

Responsibilities:

• verify engines implement IEngineContract  
• ensure engines are stateless  
• ensure engines do not reference other engines  

Example:

```csharp
public sealed class EngineArchitectureValidator
{
    public bool ValidateEngine(Type engineType)
    {
        // validation logic
    }
}
```

---

# EVENT CONTRACT

Create:

```
IEventContract.cs
```

Example:

```csharp
public interface IEventContract
{
    Guid EventId { get; }

    string EventType { get; }

    DateTime Timestamp { get; }
}
```

All events must implement this interface.

---

# EVENT VALIDATOR

Create:

```
EventSchemaValidator.cs
```

Responsibilities:

• verify events implement IEventContract  
• ensure events have immutable fields  
• ensure events contain timestamp  

---

# WORKFLOW CONTRACT

Create:

```
WorkflowArchitectureValidator.cs
```

Rules:

• workflows must call engines through dispatcher  
• workflows cannot mutate state directly  
• workflows must produce events  

---

# DETERMINISTIC BUILD VALIDATION

Create:

```
BuildDeterminismValidator.cs
```

Responsibilities:

• ensure builds produce deterministic outputs  
• prevent dynamic runtime modifications  
• validate versioning consistency  

---

# ENFORCEMENT ENGINE

Create:

```
GuardrailEnforcementEngine.cs
```

Purpose:

Run architecture validation during build.

Flow:

```
Build
 ↓
GuardrailEnforcementEngine
 ↓
ArchitectureValidators
 ↓
Pass / Fail
```

---

# UNIT TESTS

Create project:

```
tests/guardrails/
```

Tests:

```
ArchitectureRulesTests.cs
EngineValidatorTests.cs
EventValidatorTests.cs
WorkflowValidatorTests.cs
```

Test cases:

• Validate engine contract  
• Validate event schema  
• Validate architecture rules  

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/guardrails/rules
```

Return architecture rules.

Example:

```json
{
  "rules": [
    "StatelessEngines",
    "NoEngineToEngineCalls",
    "EventSourcingRequired"
  ]
}
```

---

GET

```
/dev/guardrails/validate
```

Run architecture validation.

Return:

```json
{
  "status": "valid"
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
0 warnings
0 errors
```

---

# TEST VALIDATION

Run:

```
dotnet test
```

Expected:

```
Tests:
4 passed
0 failed
```

---

# OUTPUT FORMAT

Return:

1 Files Created  
2 Repository Tree  
3 Build Result  
4 Test Result  
5 Debug Endpoints  

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 0.5 is complete when:

• architecture guardrails compile  
• engines validated against rules  
• events validated against schema  
• workflows validated  
• tests pass  
• debug endpoints respond  

End of Phase 0.5.