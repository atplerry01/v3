# WHYCESPACE WBSM v3 — WORKFLOW SYSTEM STANDARD (WSS)

Status: LOCKED
Version: WBSM v3
Scope: Workflow Orchestration Architecture
Companions: [architecture-lock.md](architecture-lock.md), [runtime-execution-model.md](runtime-execution-model.md)

---

## 1. WSS OVERVIEW

WSS (Whyce Structural System) is the workflow orchestration system. It exists in the **T1M orchestration layer**.

Workflows orchestrate execution across engines. Workflows do **not** contain domain business logic.

For architecture flow and graph execution model, see [runtime-execution-model.md](runtime-execution-model.md) sections 1 and 4.

---

## 2. WORKFLOW LOCATION & STRUCTURE

Location: `src/runtime/workflows/`

```
src/runtime/workflows/
    definition/    # WorkflowDefinition — immutable workflow structure
    graph/         # WorkflowGraph — DAG execution
    runtime/       # WorkflowRuntimeDispatcher — engine invocation
    registry/      # WorkflowRegistry — discovery & version management
    scheduler/     # WorkflowScheduler — execution timing & delays
    persistence/   # Event-sourced workflow state
    workers/       # Dispatcher worker pools
```

---

## 3. WORKFLOW COMPONENTS

| Component                 | Responsibility                      |
|---------------------------|-------------------------------------|
| WorkflowDefinition        | Immutable workflow structure        |
| WorkflowGraph             | DAG step dependencies               |
| WorkflowRegistry          | Discovery, storage, versioning      |
| WorkflowScheduler         | Execution timing, retry scheduling, timeout detection |
| WorkflowRuntimeDispatcher | Engine invocation, step tracking, state advancement |
| WorkflowInstanceRegistry  | Active instance tracking            |
| WorkflowStateTracker      | Execution state management          |

---

## 4. WORKFLOW DEFINITION

Definitions describe workflow structure and must be immutable:

| Field         | Purpose                    |
|---------------|----------------------------|
| workflowId    | Unique workflow identifier |
| steps         | Ordered step list          |
| dependencies  | Step dependency graph      |
| timeouts      | Per-step timeout config    |
| retryPolicies | Per-step retry config      |

Examples: `PropertyAcquisitionWorkflow`, `VaultContributionWorkflow`, `ProfitDistributionWorkflow`

---

## 5. WORKFLOW STEPS

Each step maps to **one execution engine**:

| Step             | Engine Tier |
|------------------|-------------|
| ValidateCapital  | T2E         |
| AllocateCapital  | T2E         |
| CreateSPV        | T2E         |
| RegisterAsset    | T2E         |

For engine invocation contract, see [engine-implementation-standard.md](engine-implementation-standard.md) section 2.

---

## 6. WORKFLOW INSTANCE

Each execution creates a workflow instance:

| Field                | Purpose                     |
|----------------------|-----------------------------|
| InstanceId           | Unique execution identifier |
| WorkflowId           | Definition reference        |
| AggregateId          | Target domain entity        |
| CurrentStep          | Active step                 |
| CompletedSteps       | Finished steps              |
| FailedSteps          | Failed steps                |
| Status               | Current state               |
| CreatedAt            | Creation timestamp          |

### Instance States

| Status        | Description                           |
|---------------|---------------------------------------|
| Pending       | Created, not yet started              |
| Running       | Actively executing steps              |
| Completed     | All steps finished successfully       |
| Failed        | Unrecoverable failure                 |
| Retrying      | Transient failure, retrying step      |
| TimedOut      | Step exceeded timeout threshold       |
| Compensating  | Saga compensation in progress         |

State transitions must be deterministic.

---

## 7. WORKFLOW LIFECYCLE EVENTS

Workflows emit events describing execution progress:

| Event                  | Trigger                     |
|------------------------|-----------------------------|
| WorkflowStarted        | Instance created            |
| WorkflowStepCompleted  | Step finished successfully  |
| WorkflowStepFailed     | Step failed                 |
| WorkflowCompleted      | All steps done              |
| WorkflowTimedOut       | Timeout threshold exceeded  |

Events must follow the [Event Fabric & Kafka Standard](event-fabric-kafka-standard.md).

---

## 8. COMPENSATION (SAGA PATTERN)

Long-running workflows must support compensation for distributed consistency:

```
Step 1: AllocateCapital    (success)
Step 2: RegisterAsset      (failure)
  -> Compensation: ReverseCapitalAllocation
```

Each step may define a compensation step. If a later step fails, completed steps are reversed in order.

---

## 9. WORKFLOW PERSISTENCE & REPLAY

Workflow state is persisted through **event sourcing**.

Workflow events are stored in the event fabric. State is reconstructible from events.

Replay process:

1. Read workflow events
2. Reconstruct instance state
3. Resume execution

Replay must be deterministic.

---

## 10. WORKFLOW OBSERVABILITY

| Metric              | Purpose                    |
|---------------------|----------------------------|
| `workflow_throughput` | Workflows processed/sec  |
| `workflow_failures`  | Failure count             |
| `step_latency`       | Per-step execution time   |
| `retry_rate`         | Retry frequency           |
| `timeout_rate`       | Timeout frequency         |

For full observability layer, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 11. WORKFLOW VERSIONING

Workflows support versioning:

| Version | Workflow                         |
|---------|----------------------------------|
| v1      | `PropertyAcquisitionWorkflow.v1` |
| v2      | `PropertyAcquisitionWorkflow.v2` |

New workflow versions must **not break existing running instances**.

---

## 12. WORKFLOW REGISTRY & DISCOVERY

Workflows must be registered. Registry entries:

| Field           | Purpose                |
|-----------------|------------------------|
| workflowId      | Unique identifier      |
| version         | Workflow version       |
| stepDefinitions | Step list with mappings |
| retryPolicy     | Default retry config   |
| timeouts        | Default timeout config |

---

## 13. WORKFLOW TESTING

Workflow definitions must include tests. No external services required.

| Scenario           | Purpose                         |
|--------------------|---------------------------------|
| Step execution     | Steps invoke correct engines    |
| Retry handling     | Transient failures recovered    |
| Timeout behavior   | Timeouts trigger correctly      |
| Compensation logic | Saga rollback works correctly   |
