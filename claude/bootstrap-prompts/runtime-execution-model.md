# WHYCESPACE WBSM v3 — RUNTIME EXECUTION MODEL

Status: LOCKED
Version: WBSM v3
Scope: Runtime Execution Lifecycle
Companion: [architecture-lock.md](architecture-lock.md)

---

## 1. RUNTIME POSITION IN ARCHITECTURE

Whycespace runtime sits between orchestration and execution:

```
Access Layer (T4A)
      ↓
Orchestration Layer (T1M)
      ↓
Runtime Execution Layer
      ↓
Execution Engines (T2E)
      ↓
Event Fabric
      ↓
Projection Services
      ↓
Evidence Anchoring (WhyceChain)
```

Runtime is **stateless orchestration infrastructure** responsible for:

- Workflow scheduling
- Engine invocation
- Event routing
- Retry management
- Failure recovery
- Partition routing

---

## 2. RUNTIME CORE COMPONENTS

| Component              | Responsibility             |
|------------------------|----------------------------|
| RuntimeDispatcher      | Central execution coordinator |
| WorkflowScheduler      | Workflow lifecycle scheduling |
| PartitionRouter        | Deterministic partition routing |
| EngineInvoker          | Engine invocation contract |
| EventPublisher         | Kafka event publishing     |
| RetryManager           | Transient failure recovery |
| TimeoutManager         | Step timeout enforcement   |
| WorkflowStateTracker   | Execution state tracking   |
| ExecutionContextFactory | Execution context creation |

Each component has a strictly defined responsibility.

---

## 3. WORKFLOW EXECUTION LIFECYCLE

All commands entering the system follow this lifecycle:

```
Command -> Command Validation -> Workflow Creation -> Workflow Graph Resolution
  -> Workflow Scheduling -> Engine Invocation -> Event Emission
  -> Projection Update -> Evidence Anchoring
```

No command can bypass the workflow system.

---

## 4. WORKFLOW GRAPH MODEL

Workflows are defined as directed graphs:

```
WorkflowDefinition
    ├─ WorkflowSteps
    ├─ StepDependencies
    └─ EngineMapping
```

Graph execution supports:

| Mode                  | Description                        |
|-----------------------|------------------------------------|
| Sequential execution  | `Step A -> Step B -> Step C`       |
| Parallel execution    | Steps B and C run concurrently     |
| Conditional branching | Step routing based on engine output |
| Retry edges           | Automatic retry on transient failure |
| Timeout edges         | Timeout-triggered transitions      |

Parallel execution example:

```
      Step A
       ↓
  ┌────┴────┐
Step B    Step C
  └────┬────┘
       ↓
      Step D
```

---

## 5. RUNTIME DISPATCHER

`RuntimeDispatcher` is the central execution coordinator.

Responsibilities:

- Consume workflow tasks
- Resolve workflow graph state
- Dispatch engines
- Track execution progress

Dispatcher must not contain business logic — it only orchestrates execution.

---

## 6. ENGINE INVOCATION MODEL

Invocation contract:

```
EngineInput -> EngineExecution -> EngineResult -> DomainEvent
```

All outputs must be events.

For engine rules and constraints, see [architecture-lock.md](architecture-lock.md) sections 3 and 19.

---

## 7. WORKFLOW STATE TRACKING

Runtime tracks workflow execution state via event sourcing:

```
WorkflowInstance
    ├─ InstanceId
    ├─ WorkflowDefinitionId
    ├─ CurrentStep
    ├─ CompletedSteps
    ├─ FailedSteps
    └─ Status
```

| Status    | Description                      |
|-----------|----------------------------------|
| Pending   | Workflow created, not yet started |
| Running   | Actively executing steps         |
| Completed | All steps finished successfully  |
| Failed    | Unrecoverable failure            |
| Retrying  | Transient failure, retrying step |
| TimedOut  | Step exceeded timeout threshold  |

---

## 8. TIMEOUT MANAGEMENT

`TimeoutManager` handles long-running steps.

Default engine execution timeout: **30 seconds**

If exceeded:

1. Step marked as `TimedOut`
2. `RetryManager` invoked
3. Or workflow fails (if retries exhausted)

Timeout events must be emitted.

---

## 9. FAILURE RECOVERY

Runtime supports full recovery:

| Strategy                      | Mechanism          |
|-------------------------------|--------------------|
| Event replay                  | Kafka log replay   |
| Workflow state reconstruction | Event sourcing     |
| Projection rebuild            | Stream replay      |

Kafka log replay rebuilds entire system state.

For retry governance, DLQ, and replay governance, see [architecture-lock.md](architecture-lock.md) sections 8-10.

---

## 10. PROJECTION UPDATE MODEL

Events trigger projections. Projection workers subscribe to event topics.

| Projection              | Purpose                   |
|-------------------------|---------------------------|
| VaultBalanceProjection  | Vault balance read model  |
| SPVCapitalProjection    | SPV capital read model    |
| AssetValueProjection    | Asset valuation read model |
| RevenueProjection       | Revenue read model        |

Projection stores: **Redis**, **Postgres**

Projections are eventually consistent.

For projection lag monitoring, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 11. EVIDENCE ANCHORING

Critical events must anchor to WhyceChain:

| Event              | Domain              |
|--------------------|---------------------|
| PolicyDecision     | Governance          |
| CapitalContribution | Capital allocation |
| RevenueRecorded    | Revenue tracking    |
| ProfitDistributed  | Profit distribution |

Anchoring pipeline:

```
Event -> EvidenceRecorder -> MerkleProofBuilder -> WhyceChainAnchor
```

---

## 12. SCALING MODEL

Runtime supports horizontal scaling via:

| Mechanism               | Purpose                  |
|-------------------------|--------------------------|
| Kafka partitions        | Event throughput scaling |
| Dispatcher worker pools | Workflow throughput      |
| Projection worker pools | Read model throughput    |

Scaling targets:

| Dispatchers | Throughput           |
|-------------|----------------------|
| 1           | 10,000 workflows/sec |
| 10          | 100,000 workflows/sec |
| 100         | 1,000,000 workflows/sec |

---

## 13. OBSERVABILITY

Runtime must emit observability metrics:

| Metric              | Purpose                    |
|---------------------|----------------------------|
| workflow_throughput  | Workflows processed/sec    |
| engine_execution_time | Engine latency            |
| retry_rate          | Retry frequency            |
| workflow_failures   | Failure count              |
| partition_lag       | Partition processing delay |

Logs must include: `WorkflowId`, `AggregateId`, `EngineName`, `ExecutionTime`

For full observability layer specification, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 14. WORKFLOW EXAMPLES

### Property Acquisition

```
AcquirePropertyCommand -> PropertyAcquisitionWorkflow
```

| Step             | Engine Tier |
|------------------|-------------|
| ValidateCapital  | T2E         |
| AllocateCapital  | T2E         |
| CreateSPV        | T2E         |
| RegisterAsset    | T2E         |
| RecordOwnership  | T2E         |

### Profit Distribution

```
RevenueRecordedEvent -> ProfitCalculationWorkflow
  -> ProfitDistributionWorkflow -> DistributionEvents
```

---

## 15. RUNTIME GUARANTEES

- Deterministic execution
- Idempotent processing
- Ordered events per aggregate
- Full event replay recovery

---

## 16. ARCHITECTURE INTEGRATION

| System          | Role                    |
|-----------------|-------------------------|
| WSS             | Workflow system          |
| WhycePolicy     | Governance enforcement   |
| WhyceID         | Identity verification    |
| WhyceChain      | Evidence integrity       |
| Economic engines | Capital and revenue flows |

---

All Whycespace systems must execute through this runtime.
No direct engine invocation is allowed outside runtime.
