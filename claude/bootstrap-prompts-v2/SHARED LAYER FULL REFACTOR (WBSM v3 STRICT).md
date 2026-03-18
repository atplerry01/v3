# WHYCESPACE — SHARED LAYER CORRECTIVE REFACTOR
# WBSM v3 STRICT MODE — FORCE CANONICAL STRUCTURE

You are operating under WBSM v3 STRICT MODE.

The previous refactor was incomplete.
Your task now is to CORRECT src/shared/ so it matches the locked canonical structure EXACTLY.

NON-NEGOTIABLE RULES:

1. NO DRIFT — structure must match the canonical tree exactly
2. NO PARTIAL MIGRATION — old folders must not remain if replaced by canonical ones
3. NO DUPLICATION — one source of truth for each model
4. PURE SHARED LAYER — no runtime logic, no business logic, no infrastructure logic
5. BUILD MUST SUCCEED

---

# OBJECTIVE

Refactor:

src/shared/

from its current partially migrated structure into the exact canonical form below.

---

# CANONICAL TARGET STRUCTURE

src/shared/
├── Whycespace.Shared.csproj
├── primitives/
│   ├── identity/
│   ├── location/
│   ├── common/
│   └── money/
├── context/
├── envelopes/
├── protocols/
├── projections/
└── contracts/
    ├── Whycespace.Contracts.csproj
    ├── commands/
    ├── engines/
    ├── events/
    ├── runtime/
    ├── workflows/
    ├── evidence/
    ├── policy/
    ├── observability/
    └── errors/

---

# STEP 1 — MOVE OLD ROOT FOLDERS INTO CANONICAL LOCATIONS

MOVE:

- src/shared/Identity/WhyceIdentity.cs
  -> src/shared/primitives/identity/WhyceIdentity.cs

- src/shared/Location/GeoLocation.cs
  -> src/shared/primitives/location/GeoLocation.cs

- src/shared/Projections/IProjection.cs
  -> src/shared/projections/IProjection.cs

DELETE EMPTY OLD FOLDERS:

- src/shared/Identity/
- src/shared/Location/
- src/shared/Projections/

---

# STEP 2 — REMOVE PRIMITIVES FROM CONTRACTS

MOVE:

- src/shared/contracts/primitives/GuidId.cs
  -> src/shared/primitives/common/GuidId.cs

- src/shared/contracts/primitives/PartitionKey.cs
  -> src/shared/primitives/common/PartitionKey.cs

- src/shared/contracts/primitives/Timestamp.cs
  -> src/shared/primitives/common/Timestamp.cs

CREATE NEW:

src/shared/primitives/common/
- CorrelationId.cs
- Version.cs

DELETE:

- src/shared/contracts/primitives/

---

# STEP 3 — CENTRALIZE ENVELOPES

REMOVE envelope models from contracts and centralize them in:

src/shared/envelopes/

MOVE / CREATE:

- EventEnvelope.cs -> src/shared/envelopes/EventEnvelope.cs
- EngineInvocationEnvelope.cs -> src/shared/envelopes/EngineInvocationEnvelope.cs
- CommandEnvelope.cs -> src/shared/envelopes/CommandEnvelope.cs
- WorkflowEnvelope.cs -> src/shared/envelopes/WorkflowEnvelope.cs

RULES:

- There must be only one EventEnvelope in the entire repo
- There must be only one EngineInvocationEnvelope in the shared layer
- contracts/events/ must NOT contain EventEnvelope.cs
- contracts/engines/ must NOT remain the source of truth for envelope wrappers

DELETE OLD DUPLICATES after move.

---

# STEP 4 — MOVE KAFKA TOPICS INTO PROTOCOLS

MOVE:

- src/shared/contracts/events/KafkaTopics.cs
  -> src/shared/protocols/messaging/KafkaTopics.cs

CREATE:

src/shared/protocols/messaging/
- TopicNamingConvention.cs
- PartitioningStrategy.cs

---

# STEP 5 — CREATE CONTEXT LAYER

CREATE:

src/shared/context/
- RuntimeContext.cs
- ExecutionContext.cs
- CorrelationContext.cs
- TenantContext.cs
- IdentityContext.cs
- RequestContext.cs

These must be metadata-only types, immutable where appropriate, with no service logic.

---

# STEP 6 — CREATE MONEY PRIMITIVES

CREATE:

src/shared/primitives/money/
- Money.cs
- Currency.cs
- ExchangeRate.cs

These are required because Whycespace is an economic system.

---

# STEP 7 — CREATE PROTOCOLS LAYER

CREATE:

src/shared/protocols/

SUBFOLDERS AND FILES:

messaging/
- KafkaTopics.cs
- TopicNamingConvention.cs
- PartitioningStrategy.cs

versioning/
- CompatibilityMode.cs
- VersionConstraint.cs

serialization/
- ISerializer.cs
- JsonSerializerAdapter.cs
- SerializationFormat.cs

idempotency/
- IdempotencyKey.cs
- IdempotencyPolicy.cs
- IdempotencyScope.cs

If Version.cs is needed for protocol versioning, reuse the shared primitives/common/Version.cs or create a thin protocol-specific wrapper only if necessary. Avoid duplication.

---

# STEP 8 — EXPAND PROJECTIONS

ENSURE:

src/shared/projections/
- IProjection.cs
- ProjectionMetadata.cs
- ProjectionVersion.cs

---

# STEP 9 — CLEAN CONTRACTS

KEEP contracts only for interfaces and cross-layer contract models.

FINAL CONTRACTS STRUCTURE MUST BE:

contracts/
├── commands/
├── engines/
├── events/
├── runtime/
├── workflows/
├── evidence/
├── policy/
├── observability/
└── errors/

DO NOT leave old folders that are no longer canonical.

---

# STEP 10 — EXPAND CONTRACTS

## commands/
ADD:
- CommandMetadata.cs
- CommandValidationResult.cs

## engines/
KEEP:
- IEngine.cs
- EngineContext.cs
- EngineResult.cs
- EngineEvent.cs

ADD:
- EngineExecutionMetadata.cs
- EngineCapability.cs

REMOVE any envelope ownership from this folder after centralizing envelopes.

## events/
KEEP:
- IEvent.cs
- EventBase.cs
- SystemEvent.cs

ADD:
- EventMetadata.cs
- EventVersion.cs

REMOVE:
- EventEnvelope.cs
- KafkaTopics.cs

## runtime/
ADD:
- RuntimeExecutionMetadata.cs

## workflows/
ADD:
- WorkflowVersion.cs

## evidence/
KEEP existing files
ADD:
- EvidenceMetadata.cs

## policy/
CREATE:
- PolicyDecision.cs
- PolicyContext.cs
- IPolicyEvaluator.cs
- PolicyEvaluationResult.cs

## observability/
CREATE:
- TraceMetadata.cs
- MetricsMetadata.cs
- DiagnosticContext.cs
- ExecutionTrace.cs

## errors/
CREATE:
- ErrorCode.cs
- ErrorDetail.cs
- SystemException.cs
- FailureReason.cs

---

# STEP 11 — CLEANUP OLD STRUCTURE

After migration, REMOVE old non-canonical structure:

- src/shared/Identity/
- src/shared/Location/
- src/shared/Projections/
- src/shared/contracts/primitives/
- src/shared/contracts/events/EventEnvelope.cs
- src/shared/contracts/events/KafkaTopics.cs

Also remove any duplicate envelope or version files left behind.

---

# STEP 12 — VALIDATION

ENSURE:

- solution builds successfully
- namespaces are updated
- no circular dependencies
- no duplicate EventEnvelope
- no duplicate KafkaTopics
- no duplicate primitive ownership
- contracts only contain contracts
- primitives are top-level
- context exists
- protocols exists
- envelopes exists

---

# STEP 13 — OUTPUT

Provide:

1. final src/shared/ folder tree
2. files moved
3. files created
4. files deleted
5. confirmation with this exact sentence:

"Shared layer is now fully corrected and WBSM v3 canonical."

---

# FINAL RULE

Do not preserve old structure for convenience.
Do not stop at partial migration.
If an old folder has been replaced by a canonical folder, remove the old one.

Proceed with FULL CORRECTIVE REFACTOR.