# WHYCESPACE WBSM v3 — ARCHITECTURE LOCK

Status: LOCKED
Version: WBSM v3
Scope: Runtime Infrastructure + Event Architecture
Applies To: Entire Whycespace System

---

## 1. PURPOSE

This document **locks the canonical architecture of Whycespace Runtime (WBSM v3)**.

It ensures:

- Architectural consistency
- Deterministic execution
- Strict separation of concerns
- Non-drifting implementation
- Production-grade reliability

All future development **must comply with this architecture**.
No architectural deviations are allowed without **constitutional amendment**.

---

## 2. SYSTEM ARCHITECTURE LAYERS

Whycespace is built using a **multi-layered architecture**:

| # | Layer               | Purpose                    |
|---|---------------------|----------------------------|
| 1 | Constitutional      | Governance invariants      |
| 2 | Economic            | Business domain models     |
| 3 | Governance          | Policy enforcement         |
| 4 | Runtime Execution   | Engine invocation & dispatch |
| 5 | Event Fabric        | Distributed event streaming |
| 6 | Reliability         | Fault tolerance & recovery |
| 7 | Observability       | Metrics & diagnostics      |
| 8 | Projection          | CQRS read models           |

---

## 3. ENGINE TAXONOMY

All engines must follow the canonical engine classification:

| Tier | Name                    |
|------|-------------------------|
| T0U  | Constitutional Engines  |
| T1M  | Orchestration Engines   |
| T2E  | Execution Engines       |
| T3I  | Intelligence Engines    |
| T4A  | Access / Interface Engines |

Engine rules:

- Engines must be stateless
- Engines must be deterministic
- Engines must be thread-safe
- Engines must not call other engines directly
- Engines must emit events instead of mutating state externally
- Engines must not persist data
- Engines must not contain domain definitions

Communication: `Engine -> Event -> Runtime -> Next Engine`
Forbidden: `Engine -> Engine`

---

## 4. RUNTIME ARCHITECTURE

Location: `src/runtime/`

The runtime is the **core execution system**. Modules:

- `command` — command execution
- `dispatcher` — runtime dispatch
- `engine` — engine invocation
- `engine-dispatch` — engine routing
- `engine-manifest` — engine registry
- `engine-workers` — worker management
- `worker-pool` — pool coordination
- `partition` / `partitions` — partition execution
- `workflow` / `workflow-runtime` — workflow orchestration

---

## 5. EVENT FABRIC

Location: `src/runtime/event-fabric/`

Core components:

- `EventEnvelope` — canonical event wrapper
- `EventRegistry` — event type registry
- `KafkaEventPublisher` — Kafka integration
- `PartitionKeyResolver` — deterministic routing
- `EventTopics` — topic governance

EventEnvelope fields:

| Field          | Purpose                   |
|----------------|---------------------------|
| EventId        | Unique event identifier   |
| EventType      | Event classification      |
| AggregateId    | Entity identifier         |
| PartitionKey   | Routing key               |
| SequenceNumber | Ordering guarantee        |
| Metadata       | Contextual information    |
| Payload        | Event data                |
| Timestamp      | Event time                |
| TraceId        | Distributed trace identifier |
| CorrelationId  | Request correlation chain |
| EventVersion   | Event schema version      |

Kafka is the **event transport layer**.

### Event Versioning Governance

- Events must be **backward compatible**
- Breaking changes require a new `EventVersion`
- `SchemaVersion` must be tracked in the `EventRegistry`
- Consumers must handle all supported versions
- Deprecated versions must have a documented sunset timeline

---

## 6. PARTITION EXECUTION MODEL

Routing strategy: `AggregateId -> Kafka Key -> Partition`

This guarantees:

- Deterministic processing
- Ordered event handling
- Horizontal scalability

Execution flow:

```
Kafka Consumer -> Partition Worker -> Runtime Dispatcher -> Engine Invocation
```

Each partition operates independently.

---

## 7. EVENT LIFECYCLE CONTROL

The event lifecycle is strictly governed:

```
Event Processing -> Retry -> Dead Letter Queue -> Replay Governance -> Replay Execution
```

Implemented modules:

- `event-idempotency` — duplicate detection
- `event-replay` — replay execution
- `reliability` — fault tolerance
- `event-observability` — metrics collection

Guarantees: no event loss, deterministic replay, safe failure recovery.

---

## 8. RETRY GOVERNANCE

Features: `RetryPolicy`, `ExponentialBackoff`, `RetryMetadata`

Rules:

- Retry attempts are bounded
- Retries must be deterministic
- Retries must not duplicate financial actions

---

## 9. DEAD LETTER QUEUE (DLQ)

Topic: `whyce.events.deadletter`

DLQ captures:

| Field         | Purpose              |
|---------------|----------------------|
| EventId       | Original event ID    |
| EventType     | Event classification |
| SourceTopic   | Origin topic         |
| Partition     | Source partition      |
| Offset        | Kafka offset         |
| FailureReason | Failure category     |
| ErrorMessage  | Error detail         |
| RetryCount    | Attempts made        |
| Payload       | Original payload     |

Guarantees: forensic event recovery, operator inspection, safe replay governance.

---

## 10. REPLAY GOVERNANCE

Topic: `whyce.events.replay`

Safeguards:

- Maximum replay count: **2**
- Payload validation required
- Event identity preserved
- Replay must remain idempotent

Replay requests are evaluated by `EventReplayGovernanceEngine`.
Events exceeding limits are **quarantined**.

---

## 11. IDEMPOTENCY LAYER

Location: `src/runtime/event-idempotency/`

Responsibilities:

- Detect duplicate events
- Block concurrent duplicates
- Guarantee single execution

Critical for: financial transactions, vault operations, capital allocation events.

---

## 12. PROJECTION SYSTEM

Location: `src/runtime/projection-runtime/`

Modules: `projection-runtime`, `projection-rebuild`, `projections`

Capabilities:

- Event projection
- Read model generation
- Projection rebuild from event streams (deterministic state reconstruction)

---

## 13. RELIABILITY LAYER

Location: `src/runtime/reliability/`

Components:

- Retry Engine
- DeadLetter Engine
- Replay Governance
- Worker Health Monitor
- Partition Circuit Breaker
- Runtime Recovery Engine

Worker health rules:

| Failures | Status    |
|----------|-----------|
| 2        | Degraded  |
| 5        | Unhealthy |

Partition protection: **10 failures within 30 seconds -> Circuit Open**

Circuit states: `Closed -> Open -> HalfOpen -> Closed`

---

## 14. FAILURE ISOLATION

- `WorkerHealthMonitor` tracks worker health
- `PartitionCircuitBreakerEngine` isolates failing partitions
- `RuntimeRecoveryEngine` restores partitions automatically

Guarantees: fault containment, runtime resilience, automatic recovery.

---

## 15. OBSERVABILITY LAYER

Location: `src/runtime/event-observability/`

Metrics: `EventMetrics`, `FailureMetrics`, `ReplayMetrics`, `PartitionMetrics`, `ProjectionMetrics`

### Projection Lag Monitoring

| Metric                       | Purpose                                  |
|------------------------------|------------------------------------------|
| `projection_lag`             | Delay between event time and projection  |
| `projection_rebuild_progress`| Rebuild completion percentage            |

Projection lag must be monitored to detect read model drift.
Alerts should fire when lag exceeds acceptable thresholds.

Endpoints:

| Endpoint                          | Purpose            |
|-----------------------------------|---------------------|
| `/dev/events/metrics`             | Event metrics       |
| `/dev/events/metrics/failures`    | Failure metrics     |
| `/dev/events/metrics/replay`      | Replay metrics      |
| `/dev/events/metrics/partitions`  | Partition metrics   |

Observability must remain **non-blocking**.

---

## 16. DEBUG AND OPERATOR ENDPOINTS

| Endpoint                          | Purpose              |
|-----------------------------------|----------------------|
| `/dev/events/dlq`                 | Dead letter queue    |
| `/dev/events/replay`              | Replay inspection    |
| `/dev/runtime/workers/health`     | Worker health        |
| `/dev/runtime/partitions/health`  | Partition health     |
| `/dev/runtime/circuit-breakers`   | Circuit breaker state |

These are **operator-only diagnostic tools**.

---

## 17. CANONICAL PROCESSING PIPELINE

```
Command -> Workflow -> Runtime Dispatcher -> Engine Invocation
   -> Event Fabric -> Partition Worker -> Projection Runtime
```

Failure path:

```
Retry -> Dead Letter Queue -> Replay Governance -> Replay Topic
```

---

## 18. SYSTEM GUARANTEES

- Deterministic execution
- Ordered entity processing
- Safe event recovery
- Distributed fault tolerance
- Financial transaction safety

---

## 19. ARCHITECTURAL CONSTRAINTS

- Engines must remain stateless
- Engines must not call other engines
- Runtime modules must not introduce direct domain mutations
- Events are the only communication mechanism between engines
- All failures must be observable
- No event may be silently discarded

---

## 20. ARCHITECTURE STATUS

WBSM v3 runtime infrastructure is **complete**.

Implemented layers:

- Event Fabric
- Execution Runtime
- Event Lifecycle Control
- Reliability Layer
- Projection System
- Observability Layer

Remaining work: **Phase 2.x** — system engines.

---

## 21. PHASE 2.x — SYSTEM ENGINES

Constitutional:

- WhycePolicy
- WhyceChain
- WhyceID
- Guardian Governance

Economic:

- Vault System
- Cluster System
- SPV System
- Revenue System
- Profit Distribution System
