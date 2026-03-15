Projection Services Implementation prompts.

## Partitioned Projection Worker Model.

## Global Event Fabric Governance Layer

## Projection Auto-Discovery + Registration

# WHYCESPACE WBSM v3
# EVENT LIFECYCLE CONTROL LAYER
# EVENT RECOVERY + DEAD LETTER QUEUE (DLQ) GOVERNANCE

You are implementing Event Recovery + Dead Letter Queue Governance for the Whycespace Runtime.

This system belongs to the Event Lifecycle Control Layer and guarantees:

- no event is ever silently lost
- failures are captured deterministically
- dead letter events preserve original payloads
- operators can inspect failures
- events can be safely replayed
- governance and financial integrity are preserved

This integrates with existing runtime modules:

src/runtime/event-fabric
src/runtime/event-replay
src/runtime/event-idempotency
src/runtime/event-observability
src/runtime/reliability
src/runtime/reliability-runtime

Follow WBSM v3 architectural rules:

- engines must be stateless
- deterministic execution only
- no persistence logic inside engines
- no engine-to-engine calls
- failures must produce evidence
- recovery must be bounded and idempotent

------------------------------------------------------------
OBJECTIVES
------------------------------------------------------------

Implement the following components:

1. Failure Classification Model
2. Dead Letter Event Model
3. Dead Letter Metadata Model
4. Dead Letter Engine
5. Dead Letter Publisher (Kafka)
6. Event Recovery Engine
7. Recovery Decision Model
8. DLQ Inspection Endpoints
9. Unit Tests

------------------------------------------------------------
LOCATION
------------------------------------------------------------

Use the existing runtime reliability modules.

Create the following folders:

src/runtime/reliability/deadletter/
src/runtime/reliability/recovery/

Structure:

src/runtime/reliability/

deadletter/
│
├── models/
│   ├── DeadLetterEvent.cs
│   ├── DeadLetterReason.cs
│   └── DeadLetterMetadata.cs
│
├── engine/
│   └── DeadLetterEngine.cs
│
└── publisher/
    └── DeadLetterPublisher.cs


recovery/
│
├── models/
│   └── RecoveryDecision.cs
│
└── engine/
    └── EventRecoveryEngine.cs

All files must remain part of:

src/runtime/Whycespace.Runtime.csproj

No new runtime project should be created.

------------------------------------------------------------
FAILURE CLASSIFICATION MODEL
------------------------------------------------------------

Create file:

DeadLetterReason.cs

namespace Whycespace.Runtime.Reliability.DeadLetter.Models;

public enum DeadLetterReason
{
    RetryLimitExceeded,
    InvalidPayload,
    SchemaViolation,
    PolicyViolation,
    EngineFailure,
    InfrastructureFailure
}

Meaning:

RetryLimitExceeded → retry attempts exhausted  
InvalidPayload → corrupted event payload  
SchemaViolation → event schema mismatch  
PolicyViolation → WHYCEPOLICY rejection  
EngineFailure → runtime engine exception  
InfrastructureFailure → runtime system failure  

------------------------------------------------------------
DEAD LETTER EVENT MODEL
------------------------------------------------------------

Create file:

DeadLetterEvent.cs

namespace Whycespace.Runtime.Reliability.DeadLetter.Models;

public sealed record DeadLetterEvent
(
    Guid EventId,
    string EventType,
    string SourceTopic,
    int Partition,
    long Offset,
    DeadLetterReason Reason,
    string ErrorMessage,
    int RetryCount,
    DateTime FailedAt,
    string Payload
);

This model captures:

- original event identity
- Kafka position
- failure reason
- error message
- retry count
- raw payload

This guarantees forensic event recovery.

------------------------------------------------------------
DEAD LETTER METADATA
------------------------------------------------------------

Create file:

DeadLetterMetadata.cs

namespace Whycespace.Runtime.Reliability.DeadLetter.Models;

public sealed record DeadLetterMetadata
(
    Guid EventId,
    int RetryCount,
    DateTime FirstFailure,
    DateTime LastFailure
);

This metadata assists recovery and replay logic.

------------------------------------------------------------
DEAD LETTER ENGINE
------------------------------------------------------------

Create file:

deadletter/engine/DeadLetterEngine.cs

Responsibilities:

- construct dead letter events
- classify failure reason
- attach metadata
- forward event to publisher

Signature:

public DeadLetterEvent CreateDeadLetterEvent(
    EventEnvelope envelope,
    DeadLetterReason reason,
    string errorMessage,
    int retryCount
)

Rules:

- engine must be stateless
- deterministic output
- no persistence
- no runtime dependencies

------------------------------------------------------------
DEAD LETTER PUBLISHER
------------------------------------------------------------

Create file:

deadletter/publisher/DeadLetterPublisher.cs

Responsibilities:

- publish dead letter events to Kafka

Kafka topic:

whyce.events.deadletter

Publisher interface:

public Task PublishAsync(DeadLetterEvent deadLetterEvent);

Kafka message key:

EventId

------------------------------------------------------------
EVENT RECOVERY ENGINE
------------------------------------------------------------

Create file:

recovery/engine/EventRecoveryEngine.cs

Responsibilities:

- determine if event may be replayed
- prevent replay loops
- enforce recovery governance

Create model:

RecoveryDecision.cs

namespace Whycespace.Runtime.Reliability.Recovery.Models;

public sealed record RecoveryDecision
(
    bool AllowReplay,
    bool Quarantine,
    string Reason
);

Example recovery logic:

if replayCount > 2
    quarantine event

if schema mismatch
    quarantine event

if retry limit exceeded
    allow replay

------------------------------------------------------------
DLQ GOVERNANCE RULES
------------------------------------------------------------

Rule 1

No event may be silently discarded

Rule 2

All failures must produce DLQ event

Rule 3

DLQ events must preserve original payload

Rule 4

DLQ events must remain observable

Rule 5

Replay must be idempotent

------------------------------------------------------------
OPERATOR DLQ ENDPOINTS
------------------------------------------------------------

Extend DebugController.

Add endpoints:

GET /dev/events/dlq  
GET /dev/events/dlq/{eventId}  
POST /dev/events/dlq/replay  

Capabilities:

- list DLQ events
- inspect DLQ event
- replay event

Replay topic:

whyce.events.replay

------------------------------------------------------------
TESTS
------------------------------------------------------------

Create tests under:

tests/runtime/reliability/

Test project:

Whycespace.Reliability.Tests.csproj

Test file:

DeadLetterEngineTests.cs

Test scenarios:

- DLQ event created when retry limit exceeded
- DLQ event created for schema violation
- DLQ event preserves payload
- DLQ event contains correct reason
- recovery engine prevents infinite replay
- recovery engine quarantines invalid events

Minimum:

10 tests

------------------------------------------------------------
VALIDATION CHECKLIST
------------------------------------------------------------

All conditions must pass:

Build succeeds  
0 warnings  
0 errors  
All tests pass  
DLQ event creation deterministic  
Engines stateless  
No persistence inside engines  
No engine-to-engine calls  
Replay governance enforced  

------------------------------------------------------------
EXPECTED RESULT
------------------------------------------------------------

After implementation the runtime will support:

- deterministic event failure capture
- dead letter queue governance
- operator inspection
- safe event replay
- financial-grade event recovery

This completes the Event Recovery layer of the Whycespace Event Lifecycle Control System.

Next lifecycle components will include:

- Event Replay Governance
- Event Observability Metrics
- Distributed Failure Monitoring