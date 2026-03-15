# WHYCESPACE WBSM v3 — EVENT FABRIC & KAFKA STANDARD

Status: LOCKED
Version: WBSM v3
Scope: Kafka Operations, Topic Governance, Event Contracts
Companions: [architecture-lock.md](architecture-lock.md), [runtime-execution-model.md](runtime-execution-model.md)

---

## 1. EVENT FABRIC POSITION IN ARCHITECTURE

The Event Fabric is the system's message backbone, connecting engines, workflows, projections, rebuild systems, observability, and evidence anchoring.

```
Command -> Workflow (T1M) -> Execution Engine (T2E) -> Event Fabric (Kafka)
  -> Projection Runtime -> Read Models -> Query APIs
```

For the canonical EventEnvelope schema, see [architecture-lock.md](architecture-lock.md) section 5.

---

## 2. EVENT IDENTITY

`EventId` must be globally unique, generated at event creation, and immutable.

Recommended format: **UUID v7** (time-ordered for natural sequencing).

---

## 3. AGGREGATE ID EXAMPLES

`AggregateId` represents the domain entity whose state changed:

| Domain   | AggregateId  |
|----------|--------------|
| Capital  | VaultId      |
| SPV      | SPVId        |
| Property | PropertyId   |
| Mobility | RideId       |
| Identity | IdentityId   |

All events must contain an AggregateId.

---

## 4. SEQUENCE NUMBERS

Events must include a sequence number per aggregate:

```
VaultCreated         -> Sequence 1
CapitalContribution  -> Sequence 2
CapitalAllocation    -> Sequence 3
```

Sequence numbers enforce ordering validation. Projection workers must ignore out-of-order events.

---

## 5. EVENT TYPE NAMING

`EventType` identifies the domain event. Must be immutable.

Examples:

| EventType                      | Domain     |
|--------------------------------|------------|
| `IdentityCreated`              | Identity   |
| `PolicyDecisionEvaluated`      | Governance |
| `CapitalContributionRecorded`  | Capital    |
| `AssetRegistered`              | Assets     |
| `RevenueRecorded`              | Revenue    |
| `ProfitDistributed`            | Profit     |

---

## 6. EVENT PAYLOAD RULES

Payload must be:

- Immutable
- A historical fact (something that already occurred)
- Free of infrastructure data

Example — `CapitalContributionRecordedEvent`:

| Field         | Type     |
|---------------|----------|
| VaultId       | Guid     |
| Amount        | decimal  |
| ContributorId | Guid     |
| Timestamp     | DateTime |

---

## 7. EVENT METADATA

Metadata contains transport and tracing information:

| Field         | Purpose                    |
|---------------|----------------------------|
| CorrelationId | Request correlation chain  |
| WorkflowId    | Originating workflow       |
| EngineId      | Producing engine           |
| SourceService | Source service identifier   |
| TraceId       | Distributed trace ID       |

`TraceId` and `CorrelationId` are also top-level EventEnvelope fields — see [architecture-lock.md](architecture-lock.md) section 5.

---

## 8. TOPIC NAMING CONVENTION

Format: `whyce.<domain>.events` (lowercase)

| Topic                    | Domain            |
|--------------------------|-------------------|
| `whyce.identity.events`  | Identity          |
| `whyce.policy.events`    | Governance        |
| `whyce.workflow.events`  | Workflow          |
| `whyce.capital.events`   | Capital           |
| `whyce.asset.events`     | Assets            |
| `whyce.revenue.events`   | Revenue           |
| `whyce.profit.events`    | Profit            |
| `whyce.cluster.events`   | Cluster           |

System topics (from [architecture-lock.md](architecture-lock.md)):

| Topic                       | Purpose          |
|-----------------------------|------------------|
| `whyce.events.deadletter`   | DLQ              |
| `whyce.events.replay`       | Replay governance |

---

## 9. PRODUCER RULES

Execution engines must **never** publish directly to Kafka.

Publishing flow:

```
EngineResult -> EventPublisher (KafkaEventPublisher) -> Kafka Producer
```

This ensures consistent event envelopes across the platform.

---

## 10. CONSUMER GROUPS

Consumers must use Kafka consumer groups:

| Consumer Group        | Purpose                    |
|-----------------------|----------------------------|
| `projection-workers`  | Projection processing      |
| `projection-rebuild`  | Replay & rebuild           |
| `analytics-workers`   | Analytics pipeline         |
| `observability-workers` | Metrics & monitoring     |

Consumer groups enable parallel event processing across partitions.

---

## 11. EVENT RETENTION

Kafka retention: **7–30 days** (recommended).

Long-term event storage may be archived separately for compliance or audit purposes.

---

## 12. EXACTLY-ONCE SEMANTICS

Kafka delivery is **at-least-once**. System correctness is achieved through:

- Idempotent consumers (EventId tracking)
- Deterministic projections
- Sequence number validation

For idempotency infrastructure, see [architecture-lock.md](architecture-lock.md) section 11.

---

## 13. EVENT SECURITY

Events must never expose sensitive information:

- Private keys
- Credentials
- Policy secrets

Sensitive data must be encrypted or excluded from event payloads.

---

## 14. EVENT REGISTRY & DISCOVERY

Event schemas must be registered in `EventRegistry`:

| Registry Maps | Purpose               |
|---------------|-----------------------|
| Event types   | Type classification   |
| Topics        | Topic routing         |
| Schemas       | Schema governance     |

For event versioning and schema evolution, see [architecture-lock.md](architecture-lock.md) section 5 (Event Versioning Governance).

---

## 15. EVENT TESTING

Event producers must include tests validating:

| Test Case                | Purpose                      |
|--------------------------|------------------------------|
| Envelope structure       | Correct EventEnvelope fields |
| Partition routing        | AggregateId-based routing    |
| Sequence number correctness | Ordering guarantees       |
