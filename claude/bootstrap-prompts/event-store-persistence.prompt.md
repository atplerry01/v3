# PROMPT
# WHYCESPACE WBSM v3
# EVENT STORE & PERSISTENCE ARCHITECTURE SPECIFICATION

------------------------------------------------------------
SECTION 1 — CONTEXT
------------------------------------------------------------

You are working inside the Whycespace WBSM v3 architecture.

Before producing the document you MUST study and respect the following canonical documents:

- architecture-lock.md
- implementation-guardrails.md
- runtime-execution-model.md
- event-fabric-kafka-standard.md
- projection-read-model-standard.md

These documents define the system architecture.

The Whycespace runtime follows:

Command → Workflow → Engine → Event → Projection

Events are the authoritative record of system state.

Your task is to define the canonical **Event Store & Persistence Architecture** that governs:

• event storage
• aggregate persistence
• snapshot strategy
• projection rebuild sources
• archival strategy
• long-term audit integrity

This specification must integrate with the existing runtime architecture.

This document will become a **canonical lock document**.

------------------------------------------------------------
SECTION 2 — ARCHITECTURE RULES
------------------------------------------------------------

You must enforce the rules from:

architecture-lock.md
implementation-guardrails.md

Critical rules:

• engines are stateless
• engines do not persist state
• events represent the source of truth
• projections are read-only materialized views
• event communication is the only inter-engine communication

Persistence must support:

• deterministic replay
• event ordering
• aggregate reconstruction
• audit-grade history

Persistence must not violate:

domain → system → engines → runtime dependency direction.

------------------------------------------------------------
SECTION 3 — TARGET COMPONENT
------------------------------------------------------------

Generate the canonical architecture document:

WHYCESPACE EVENT STORE & PERSISTENCE STANDARD

This specification must define the persistence model for:

• domain aggregates
• event history
• workflow state
• projections
• archival storage

------------------------------------------------------------
SECTION 4 — IMPLEMENTATION REQUIREMENTS
------------------------------------------------------------

The document must define the following architecture sections.

1 — Persistence Philosophy

Define why Whycespace uses:

Event Sourcing + CQRS

Explain:

Event = source of truth
Projections = derived state

2 — Event Store Architecture

Define:

EventStore responsibilities

Event persistence
Event ordering
Event replay source
Aggregate history storage

Define the EventStore data model:

EventId
AggregateId
SequenceNumber
EventType
EventVersion
Payload
Metadata
Timestamp

3 — Aggregate Reconstruction

Define how aggregates rebuild state:

Aggregate reconstruction flow:

Event Stream → Aggregate Rebuild → Engine Execution

4 — Snapshot Strategy

Define snapshot governance:

Snapshot interval
Snapshot schema
Snapshot storage

Example:

Every 100 events per aggregate.

Snapshots accelerate aggregate reconstruction.

5 — Event Storage Backend

Define recommended storage architecture.

Example:

Primary store:
Postgres EventStore

Columns:

AggregateId
SequenceNumber
EventData
Metadata
CreatedAt

Indexing rules must be defined.

6 — Event Archival

Define archival lifecycle:

Hot storage
Warm storage
Cold archive

Example:

Hot: Kafka
Warm: Postgres EventStore
Cold: Object storage

7 — Replay Architecture

Define replay sources:

Kafka replay (short term)
EventStore replay (long term)

Explain deterministic rebuild rules.

8 — Workflow State Persistence

Define persistence strategy for workflow instances.

Example:

Workflow events stored in EventStore.

Workflow state reconstructed through event sourcing.

9 — Audit Integrity

Explain how event history integrates with:

WhyceChain evidence anchoring.

Critical events must anchor Merkle proofs.

10 — Data Retention Policy

Define:

event retention
archive policy
legal compliance support

------------------------------------------------------------
SECTION 5 — VALIDATION RULES
------------------------------------------------------------

The document must satisfy:

• consistency with architecture-lock.md
• compatibility with event-fabric-kafka-standard.md
• compatibility with projection-read-model-standard.md
• deterministic replay guarantees
• event ordering guarantees

The document must NOT introduce:

• direct database writes from engines
• mutable aggregate state storage
• runtime state mutation outside events

------------------------------------------------------------
SECTION 6 — EXPECTED OUTPUT
------------------------------------------------------------

Output must be a full canonical specification document:

WHYCESPACE WBSM v3 — EVENT STORE & PERSISTENCE STANDARD

The document must contain:

• clear architecture explanation
• storage architecture diagrams
• replay architecture
• snapshot rules
• archival model
• data retention governance

The document must be written as a **LOCKED canonical architecture standard**.

Output must be complete and production-grade.