# WHYCESPACE WBSM v3 — EVENT STORE & PERSISTENCE STANDARD

Status: LOCKED
Version: WBSM v3
Scope: Event Persistence, Aggregate Reconstruction, Archival
Companions: [architecture-lock.md](architecture-lock.md), [event-fabric-kafka-standard.md](event-fabric-kafka-standard.md), [projection-read-model-standard.md](projection-read-model-standard.md)

---

## 1. PERSISTENCE PHILOSOPHY

Whycespace uses **Event Sourcing + CQRS**:

- **Events are the source of truth** — all state changes are captured as immutable events
- **Projections are derived state** — read models are materialized from event streams
- **Aggregates are reconstructed** — current state is rebuilt by replaying events

No mutable aggregate state storage. No direct database writes from engines.

---

## 2. EVENT STORE ARCHITECTURE

Location: `src/infrastructure/event-store/`

The EventStore is the **long-term persistence layer** for all domain events.

Responsibilities:

| Responsibility         | Description                        |
|------------------------|------------------------------------|
| Event persistence      | Durable append-only event storage  |
| Event ordering         | Sequence-guaranteed per aggregate  |
| Replay source          | Long-term replay beyond Kafka retention |
| Aggregate history      | Complete event history per entity  |

---

## 3. EVENT STORE DATA MODEL

Primary store: **Postgres**

| Column         | Type          | Purpose                    |
|----------------|---------------|----------------------------|
| EventId        | UUID          | Unique event identifier    |
| AggregateId    | UUID          | Entity identifier          |
| SequenceNumber | BIGINT        | Per-aggregate ordering     |
| EventType      | VARCHAR       | Event classification       |
| EventVersion   | INT           | Schema version             |
| Payload        | JSONB         | Serialized event data      |
| Metadata       | JSONB         | Transport/trace information |
| CreatedAt      | TIMESTAMPTZ   | Persistence timestamp      |

Indexing rules:

| Index                              | Purpose                     |
|------------------------------------|-----------------------------|
| `PRIMARY (EventId)`               | Unique event lookup         |
| `UNIQUE (AggregateId, SequenceNumber)` | Ordering guarantee     |
| `INDEX (AggregateId)`             | Aggregate stream queries    |
| `INDEX (EventType)`               | Event type filtering        |
| `INDEX (CreatedAt)`               | Time-range queries          |

For canonical EventEnvelope fields, see [architecture-lock.md](architecture-lock.md) section 5.

---

## 4. AGGREGATE RECONSTRUCTION

Aggregates rebuild state from their event stream:

```
EventStore Query (AggregateId) -> Event Stream -> Aggregate Rebuild -> Engine Execution
```

Reconstruction rules:

- Events are applied in `SequenceNumber` order
- Reconstruction must be deterministic
- Reconstructed state is never persisted — it exists only in memory during engine execution

---

## 5. SNAPSHOT STRATEGY

Snapshots accelerate aggregate reconstruction for entities with long event histories.

| Parameter          | Value                          |
|--------------------|--------------------------------|
| Snapshot interval  | Every **100 events** per aggregate |
| Snapshot storage   | Postgres (separate table)      |
| Snapshot schema    | AggregateId, SequenceNumber, State (JSONB), CreatedAt |

Reconstruction with snapshots:

```
Load Latest Snapshot -> Load Events After Snapshot SequenceNumber -> Apply Events -> Current State
```

Snapshots are **optimization only** — the system must function correctly without them via full replay.

---

## 6. STORAGE TIERING

| Tier   | Store          | Retention     | Purpose                         |
|--------|----------------|---------------|---------------------------------|
| Hot    | Kafka          | 7–30 days     | Real-time event streaming       |
| Warm   | Postgres EventStore | Indefinite | Long-term queryable persistence |
| Cold   | Object Storage | Archive       | Compliance & audit archive      |

Data flow:

```
Engine -> Event Fabric (Kafka) -> EventStore (Postgres) -> Archive (Object Storage)
```

For Kafka retention policy, see [event-fabric-kafka-standard.md](event-fabric-kafka-standard.md) section 11.

---

## 7. REPLAY ARCHITECTURE

Two replay sources depending on time horizon:

| Source              | Time Horizon  | Use Case                      |
|---------------------|---------------|-------------------------------|
| Kafka               | Short-term    | Consumer restart, offset replay |
| Postgres EventStore | Long-term     | Full projection rebuild, audit |

Replay rules:

- Replay must produce **deterministic** results
- Replay must respect event ordering per aggregate
- Replay must be idempotent

For projection rebuild infrastructure, see [projection-read-model-standard.md](projection-read-model-standard.md) section 10.

---

## 8. WORKFLOW STATE PERSISTENCE

Workflow state is persisted through **event sourcing** — not mutable database records.

| Event                 | Stored In          |
|-----------------------|--------------------|
| WorkflowStarted       | EventStore         |
| WorkflowStepCompleted | EventStore         |
| WorkflowStepFailed    | EventStore         |
| WorkflowCompleted     | EventStore         |

Workflow instance state is reconstructed by replaying workflow events.

For workflow state model, see [workflow-system-standard.md](workflow-system-standard.md) section 6.

---

## 9. AUDIT INTEGRITY

Critical events must anchor to WhyceChain for tamper-proof audit trails:

```
Event -> EvidenceRecorder -> MerkleProofBuilder -> WhyceChainAnchor
```

Events anchored:

| Event                       | Domain              |
|-----------------------------|---------------------|
| PolicyDecisionEvaluated     | Governance          |
| CapitalContributionRecorded | Capital             |
| RevenueRecorded             | Revenue             |
| ProfitDistributed           | Profit distribution |

The EventStore combined with WhyceChain provides **audit-grade history**.

For evidence anchoring pipeline, see [runtime-execution-model.md](runtime-execution-model.md) section 11.

---

## 10. DATA RETENTION POLICY

| Data Type        | Retention         | Governance                    |
|------------------|-------------------|-------------------------------|
| Kafka events     | 7–30 days         | Configurable per topic        |
| EventStore       | Indefinite        | Primary source of truth       |
| Snapshots        | Latest per aggregate | Pruned on rebuild           |
| Cold archive     | Per legal requirement | Compliance-driven           |

Legal compliance:

- Event history must support regulatory audit requirements
- Archived events must remain replayable
- Deletion must follow data governance policies (right-to-erasure via crypto-shredding where required)
