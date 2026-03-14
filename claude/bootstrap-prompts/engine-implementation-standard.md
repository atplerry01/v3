# WHYCESPACE WBSM v3 — ENGINE IMPLEMENTATION STANDARD

Status: LOCKED
Version: WBSM v3
Scope: Engine Implementation Patterns & C# Contracts
Companions: [architecture-lock.md](architecture-lock.md), [implementation-guardrails.md](implementation-guardrails.md)

---

## 1. ENGINE FILE STRUCTURE

Each engine must include the following files:

| File                  | Required | Purpose                  |
|-----------------------|----------|--------------------------|
| `EngineName.cs`       | Yes      | Engine implementation    |
| `EngineInput.cs`      | Yes      | Immutable input record   |
| `EngineResult.cs`     | Yes      | Immutable result record  |
| `EngineTests.cs`      | Yes      | Unit tests               |
| `EngineEvent.cs`      | No       | Domain event definitions |
| `EngineValidator.cs`  | No       | Input validation         |

Example:

```
src/engines/T2E/VaultContribution/
    VaultContributionEngine.cs
    VaultContributionInput.cs
    VaultContributionResult.cs
    VaultContributionTests.cs
```

For engine tier classification and placement rules, see [architecture-lock.md](architecture-lock.md) section 3 and [implementation-guardrails.md](implementation-guardrails.md) section 7.

---

## 2. ENGINE CLASS RULES

Engine classes must be `sealed`, stateless, and deterministic.

```csharp
public sealed class VaultContributionEngine
{
    public EngineResult Execute(EngineInput input)
    {
        // deterministic logic only
    }
}
```

- Single `Execute` method — ensures runtime can invoke engines consistently
- No state stored between executions
- No constructor dependencies on infrastructure

---

## 3. INPUT MODEL

Inputs must be immutable records containing only primitives or domain identifiers.

```csharp
public sealed record VaultContributionInput(
    Guid AggregateId,
    decimal Amount,
    DateTime Timestamp
);
```

Inputs must not contain infrastructure references.

---

## 4. RESULT MODEL

Results must be immutable records containing domain events for state transitions.

```csharp
public sealed record EngineResult(
    bool Success,
    string Message,
    IReadOnlyList<IDomainEvent> Events
);
```

---

## 5. ERROR HANDLING

Engines must not throw infrastructure exceptions. Errors are returned through `EngineResult`:

```csharp
return new EngineResult(
    false,
    "Invalid contribution amount",
    Array.Empty<IDomainEvent>()
);
```

---

## 6. INPUT VALIDATION

Optional validator for pre-execution validation:

```csharp
public sealed class VaultContributionValidator
{
    public bool Validate(VaultContributionInput input)
    {
        return input.Amount > 0;
    }
}
```

Validation must not depend on infrastructure.

---

## 7. DOMAIN EVENT RULES

Events emitted by engines must be immutable records representing facts that already occurred:

```csharp
public sealed record CapitalContributionRecordedEvent(
    Guid EventId,
    Guid VaultId,
    decimal Amount,
    DateTime Timestamp
);
```

Events are returned in `EngineResult.Events` — engines only produce them, runtime publishes them.

For the canonical EventEnvelope schema (including `TraceId`, `CorrelationId`, `EventVersion`), see [architecture-lock.md](architecture-lock.md) section 5.

---

## 8. ENGINE IDEMPOTENCY

Engines must be idempotent. If the same input is processed twice, the same result must occur.

This ensures safe retry handling via the runtime reliability layer.

---

## 9. ENGINE IDENTITY & REGISTRY

Each engine must have a unique identifier used for workflow mapping, runtime invocation, and observability:

```json
{
    "engineId": "VaultContributionEngine",
    "tier": "T2E",
    "cluster": "Economic"
}
```

Engines must be registered in the engine registry (`EngineManifest`).

---

## 10. ENGINE VERSIONING

Engines support versioning through workflow mapping:

| Version | Engine                        |
|---------|-------------------------------|
| v1      | `VaultContributionEngine.v1`  |
| v2      | `VaultContributionEngine.v2`  |

Runtime resolves the correct version via the workflow definition.

---

## 11. ENGINE SECURITY BOUNDARY

Engines must not implement authentication or authorization.

Security is enforced by `WhycePolicy` and `WhyceID` before engine invocation.

Engines assume validated input.

---

## 12. ENGINE OBSERVABILITY

Engines must emit execution metrics through runtime:

| Metric                  | Purpose              |
|-------------------------|----------------------|
| `engine_execution_time` | Execution latency    |
| `engine_success_rate`   | Success percentage   |
| `engine_failure_rate`   | Failure percentage   |

For full observability specification, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 13. TEST REQUIREMENTS

Each engine must include unit tests. No external services allowed in tests.

Minimum test cases:

| Scenario         | Purpose                              |
|------------------|--------------------------------------|
| Success          | Valid input produces correct result   |
| Invalid input    | Bad input returns failure result     |
| Idempotency      | Same input twice yields same output  |
| Edge cases       | Boundary conditions handled          |

For engine behavioral constraints (statelessness, no infrastructure, no engine-to-engine calls), see [architecture-lock.md](architecture-lock.md) sections 3 and 19.
