# Whycespace WBSM v3 Architecture

## System Layers

Whycespace is organized into three system layers under `src/system/`:

```
Upstream    → Governance, identity, constitutional enforcement
Midstream   → Orchestration, economic coordination, intelligence, planning
Downstream  → Clusters, SPVs, economic systems
```

## Component Map

```
src/
├── shared/      Core contracts (IEngine, EngineResult, events, workflows)
├── engines/     Stateless engine tiers (T0U → T4A)
├── runtime/     Dispatcher, workflow orchestration, projections, reliability
├── system/      Upstream / Midstream / Downstream system layers
├── domain/      Domain models (economic, mobility, property)
└── platform/    API gateway, operator console, debug endpoints
```

## Engine Tiers

| Tier | Code       | Purpose                |
|------|------------|------------------------|
| T0U  | Constitutional | Governance & policy  |
| T1M  | Orchestration  | Scheduling & routing |
| T2E  | Execution      | Domain processing    |
| T3I  | Intelligence   | Decision & matching  |
| T4A  | Access         | Auth & authorization |

## Data Flow

```
UseCase → Command → Workflow → Runtime Dispatcher → Engine → Event
```

All state mutations follow this deterministic command pipeline.

## Engine Exchange Contract

Every engine implements `IEngine`:

```csharp
public interface IEngine
{
    string Name { get; }
    Task<EngineResult> ExecuteAsync(EngineContext context);
}
```

Engines receive an `EngineInvocationEnvelope`, are invoked via the `RuntimeDispatcher`, and return `EngineResult` containing success status, events, and output data.

## Guardrails

- Engines are stateless — they cannot call other engines or access workflow state
- Workflows define graphs only — no business logic
- Clusters do not contain domain models
- Decision engines read projections only — no direct database access
- All domain models are immutable records
