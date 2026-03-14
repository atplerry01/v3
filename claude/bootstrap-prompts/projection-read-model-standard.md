# WHYCESPACE WBSM v3 — PROJECTION & READ MODEL STANDARD

Status: LOCKED
Version: WBSM v3
Scope: Projection System Architecture & Implementation
Companions: [architecture-lock.md](architecture-lock.md), [runtime-execution-model.md](runtime-execution-model.md)

---

## 1. PROJECTION POSITION IN ARCHITECTURE

Projections implement the read side of Whycespace's CQRS + Event Sourcing architecture.

- Write operations occur through engines and events
- Read operations occur through projections

The projection system consists of three runtime modules:

| Module               | Location                             | Responsibility                    |
|----------------------|--------------------------------------|-----------------------------------|
| projection-runtime   | `src/runtime/projection-runtime/`    | Event consumption & processing    |
| projection-rebuild   | `src/runtime/projection-rebuild/`    | Replay & rebuild infrastructure   |
| projections          | `src/runtime/projections/`           | Projection definitions & models   |

---

## 2. CANONICAL PROJECTION FLOW

```
Execution Engines (T2E) -> Event Fabric (Kafka) -> Projection Runtime Workers
  -> Projection Processors -> Projection Stores -> Read Models -> Query APIs
```

CQRS boundary rules:

- Projections must **never** affect the command side
- Projections must **never** emit domain events

---

## 3. PROJECTION RUNTIME

Location: `src/runtime/projection-runtime/`

```
projection-runtime/
    runtime/       # ProjectionEngine — event routing, ordering, retries
    workers/       # ProjectionWorker — processing loops, consumer lifecycle
    registry/      # ProjectionRegistry, ProcessorDiscovery, ProcessorAttribute
    storage/       # IProjectionStore, IIdempotentProjectionStore, ProjectionStateStore
    stores/        # Redis and Postgres store implementations
    models/        # ProjectionRecord
```

Projection runtime contains **infrastructure only** — no domain logic.

### ProjectionEngine

Routes events to processors, enforces ordering, handles retries, calls projection stores. Must be stateless.

### ProjectionWorker

Runs projection processing loops. Reads events from Kafka, sends to ProjectionEngine, manages consumer lifecycle. Must support horizontal scaling.

### Kafka Consumers (planned)

```
consumers/
    KafkaProjectionConsumer.cs
```

Responsibilities: subscribe to Kafka topics, manage offsets, forward events to workers. Must not contain business logic.

---

## 4. PROJECTION STORES

| Store    | Use Case                              |
|----------|---------------------------------------|
| Redis    | Real-time queries, high-frequency lookups |
| Postgres | Analytical queries, large dataset queries |

Structure:

```
stores/
    redis/
        RedisIdempotentProjectionStore.cs
    postgres/
        PostgresProjectionStore.cs
```

### Idempotency

Projections must be idempotent. Enforced through `IIdempotentProjectionStore`.

Duplicate events must not corrupt read models.

For event-level idempotency, see [architecture-lock.md](architecture-lock.md) section 11.

---

## 5. PROJECTION DEFINITIONS

Location: `src/runtime/projections/`

```
projections/
    contracts/     # IProjection, IProjectionProcessor, ProjectionEvent
    registry/      # IProjectionRegistry, ProjectionRegistry
    queries/       # ProjectionQueryService
    metrics/       # ProjectionMetrics
    core/          # System-wide projections
    clusters/      # Cluster-specific projections
    shared/
```

Projection definitions must not contain infrastructure logic.

---

## 6. CORE PROJECTIONS

Location: `src/runtime/projections/core/`

System-wide projections supporting the economic infrastructure:

| Projection              | Model              | Domain           |
|-------------------------|--------------------|------------------|
| VaultBalanceProjection  | VaultBalanceModel  | Capital tracking |
| RevenueProjection       | —                  | Revenue tracking |
| ProviderProjection      | ProviderModel      | Provider state   |

---

## 7. CLUSTER PROJECTIONS

Location: `src/runtime/projections/clusters/`

Cluster-specific read models aligned with the Whycespace cluster taxonomy:

| Cluster  | Projection                | Model                |
|----------|---------------------------|----------------------|
| Mobility | DriverLocationProjection  | DriverLocationModel  |
| Mobility | RideStatusProjection      | RideStatusModel      |
| Property | PropertyListingProjection | PropertyListingModel |

Cluster projections must respect cluster isolation — no cross-cluster dependencies.

---

## 8. READ MODEL RULES

Read models must be:

- Immutable records
- Free of domain logic
- Query-optimized data structures

---

## 9. QUERY SERVICES

`ProjectionQueryService` exposes read models. Query services must:

- Never execute business logic
- Only retrieve read models from projection stores

---

## 10. PROJECTION REBUILD

Location: `src/runtime/projection-rebuild/`

```
projection-rebuild/
    reader/        # EventLogReader — sequential event streaming, offset management
    rebuild/       # ProjectionRebuildEngine — deterministic replay
    reset/         # ProjectionResetService — safe store clearing
    checkpoints/   # ProjectionCheckpointStore — replay progress tracking
    models/        # ProjectionCheckpoint, RebuildStatus
    controller/    # ProjectionReplayController — operator interface
```

Rebuild must be **deterministic** — replaying the same events must produce identical read models.

Checkpoints enable replay recovery after interruption.

---

## 11. PROJECTION METRICS

| Metric                    | Purpose                       |
|---------------------------|-------------------------------|
| `projection_lag`          | Event-to-projection delay     |
| `event_processing_rate`   | Events processed per second   |
| `projection_errors`       | Processing error count        |
| `projection_latency`      | End-to-end projection time    |

For projection lag monitoring and alerting, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 12. PROJECTION TESTING

Projection processors must include tests. No Kafka dependency in tests.

| Scenario            | Purpose                          |
|---------------------|----------------------------------|
| Event handling      | Correct projection from events   |
| Duplicate events    | Idempotency verification         |
| Ordering correctness | Sequential processing guarantee |
| Replay correctness  | Deterministic rebuild            |

For event ordering guarantees via partition routing, see [architecture-lock.md](architecture-lock.md) section 6.
