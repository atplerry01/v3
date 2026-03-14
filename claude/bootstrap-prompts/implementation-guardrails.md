# WHYCESPACE WBSM v3 — IMPLEMENTATION GUARDRAILS

Status: CANONICAL
Version: WBSM v3
Scope: AI & Developer Implementation Rules
Companion: [architecture-lock.md](architecture-lock.md)

---

## 1. PRIMARY PRINCIPLE

Before generating any code you MUST read and enforce these guardrails.
If any rule cannot be satisfied — **STOP and request clarification**.
You must never guess.

---

## 2. LAYER DEPENDENCY RULES

```
domain      -> (pure, no outward dependencies)
system      -> (pure, no outward dependencies)
engines     -> domain, system
runtime     -> engines
platform    -> runtime
infrastructure -> external systems
```

Forbidden dependencies:

- `domain -> engines | runtime`
- `system -> engines | runtime`
- `clusters -> other clusters`

---

## 3. CANONICAL REPOSITORY STRUCTURE

```
v3/
  claude/                  # Architecture & guardrail docs
  docs/
  infrastructure/
  scripts/
  simulation/
  src/
    domain/                # Business structures only
    engines/               # Runtime logic (tiered T0U-T4A)
    runtime/               # Orchestration layer
    platform/              # Access layer (API, operator tools)
    shared/                # Cross-cutting utilities
    system/                # Runtime models
  tests/
  .github/workflows/
```

You are NOT allowed to:

- Create new top-level folders
- Move architectural layers
- Invent directory structures

If a path is unclear — **STOP and ask**.

---

## 4. DOMAIN LAYER

Location: `src/domain/`

Contains business structures only.

Allowed: entities, aggregates, value objects, domain services, domain policies.
Forbidden: runtime engines, database logic, stores, workflow logic, controllers.

Internal structure:

```
src/domain/
  core/        # Shared primitives (Entity, AggregateRoot, DomainEvent, ValueObject, Identifiers)
  clusters/    # Economic sectors (bounded contexts)
  application/
  events/
  shared/
```

---

## 5. CLUSTER RULES

Location: `src/domain/clusters/`

Example clusters: `WhyceMobility`, `WhyceProperty`, `WhyceEnergy`, `WhyceAssets`

Cluster internal structure:

```
ClusterAdministration
ClusterProviders
SubClusters
SPVs
```

Clusters must remain **bounded contexts**.
Clusters must NEVER depend on other clusters.

---

## 6. SYSTEM LAYER

Location: `src/system/`

Contains runtime models: `WorkflowState`, `RetryDecision`, `TimeoutEntry`, `EventEnvelope`, `EngineManifest`

Must NEVER contain: runtime engines, business logic, database logic.
System models are shared by engines and runtime.

---

## 7. ENGINE LAYER

Location: `src/engines/`

Path convention: `src/engines/{Tier}/{Name}/`
Example: `src/engines/T1M/WSS/`

For engine taxonomy and rules, see [architecture-lock.md](architecture-lock.md) sections 3 and 19.

---

## 8. STORE RULES

Stores maintain runtime state and must exist inside the engine layer.

Location: `src/engines/{Tier}/{Name}/stores/`

Stores may contain: `ConcurrentDictionary`, runtime tracking, in-memory cache.
Stores must NOT contain business logic.

---

## 9. RUNTIME LAYER

Location: `src/runtime/`

Responsibilities: engine invocation, workflow scheduling, distributed execution, event routing, partition routing.

Key components: `RuntimeDispatcher`, `PartitionRouter`, `WorkerPools`, `EventFabricConnector`

---

## 10. PLATFORM LAYER

Location: `src/platform/`

Provides system access: API controllers, operator control plane, developer tools.

Examples: `DebugController`, `CommandController`, `OperatorController`

---

## 11. INFRASTRUCTURE LAYER

Location: `src/infrastructure/`

External integrations: Postgres, Redis, Kafka, Monitoring, Logging.

---

## 12. WORKFLOW ORCHESTRATION

WSS orchestrates workflow execution:

```
WorkflowLifecycleEngine -> RuntimeDispatcher -> PartitionRouter
  -> WorkerPools -> ExecutionEngines
```

WSS coordinates execution but **never performs business logic**.

---

## 13. FILE GENERATION RULES

Before generating ANY file:

1. Print the target file path
2. Validate the path matches canonical architecture
3. Confirm correct layer placement

If mismatch occurs — **STOP and ask the user**.

---

## 14. CODE GENERATION SAFETY

You must never:

- Create hidden dependencies
- Mix runtime and domain logic
- Create cross-cluster imports
- Place engines in the wrong layer

If uncertain — **STOP and request clarification**.

---

## 15. VALIDATION CHECKLIST

Before finishing implementation, verify:

- [ ] No engines inside `src/system/`
- [ ] No models inside `src/engines/`
- [ ] Cluster isolation respected
- [ ] Engines stateless
- [ ] Stores isolated
- [ ] Build succeeds with 0 warnings, 0 errors
- [ ] All tests pass
- [ ] Architecture remains unchanged
