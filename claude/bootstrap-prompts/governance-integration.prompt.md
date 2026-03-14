# PROMPT
# WHYCESPACE WBSM v3
# GOVERNANCE INTEGRATION ARCHITECTURE SPECIFICATION

------------------------------------------------------------
SECTION 1 — CONTEXT
------------------------------------------------------------

You are working inside the Whycespace WBSM v3 architecture.

Before generating the document you MUST review the canonical architecture documents:

- architecture-lock.md
- runtime-execution-model.md
- workflow-system-standard.md
- event-fabric-kafka-standard.md

These documents define the runtime pipeline.

The current pipeline is:

Command → Workflow → Engine → Event → Projection

Whycespace governance systems must integrate into this pipeline.

The governance systems include:

• WhycePolicy (policy-as-code enforcement)
• WhyceID (identity & authorization)
• WhyceChain (evidence integrity)

Your task is to define the **Governance Integration Architecture** that governs how these systems integrate into runtime execution.

This specification becomes a **canonical architecture document**.

------------------------------------------------------------
SECTION 2 — ARCHITECTURE RULES
------------------------------------------------------------

You must enforce the rules from:

architecture-lock.md
implementation-guardrails.md

Critical governance principles:

• engines must not implement authorization logic
• governance must occur before engine execution
• governance decisions must be observable
• governance decisions must emit events

The governance layer must remain **orthogonal to business logic**.

------------------------------------------------------------
SECTION 3 — TARGET COMPONENT
------------------------------------------------------------

Generate the canonical document:

WHYCESPACE GOVERNANCE INTEGRATION STANDARD

This document must define how governance systems integrate with:

• command processing
• workflow execution
• engine invocation
• event integrity

------------------------------------------------------------
SECTION 4 — IMPLEMENTATION REQUIREMENTS
------------------------------------------------------------

The document must define the following sections.

1 — Governance Architecture Overview

Explain the role of:

WhycePolicy
WhyceID
WhyceChain

Explain their placement in the architecture.

2 — Governance Execution Pipeline

Define the canonical pipeline:

Command
 ↓
Identity Validation (WhyceID)
 ↓
Authorization (WhycePolicy)
 ↓
Workflow Creation
 ↓
Engine Execution
 ↓
Event Emission
 ↓
Evidence Anchoring (WhyceChain)

3 — Identity Enforcement

Explain:

Identity verification
Role validation
Service identity validation

Define IdentityContext propagation.

4 — Policy Evaluation

Define policy evaluation model.

Example:

PolicyInput
PolicyEvaluation
PolicyDecision

Policy decisions must emit events.

Example:

PolicyDecisionEvaluated.

5 — Governance Events

Define governance event types.

Examples:

IdentityVerified
PolicyDecisionEvaluated
PolicyViolationDetected
GovernanceAuditRecorded

6 — Evidence Anchoring

Define how critical events anchor to WhyceChain.

Example pipeline:

Event
 ↓
EvidenceRecorder
 ↓
MerkleProofBuilder
 ↓
WhyceChainAnchor

7 — Governance Failure Handling

Define failure cases.

Example:

PolicyViolation
IdentityFailure
AuthorizationFailure

These must prevent workflow execution.

8 — Governance Observability

Define governance metrics:

policy_decisions
policy_denials
identity_failures
governance_latency

------------------------------------------------------------
SECTION 5 — VALIDATION RULES
------------------------------------------------------------

The document must satisfy:

• compatibility with runtime-execution-model.md
• compatibility with workflow-system-standard.md
• compatibility with event-fabric-kafka-standard.md

The governance integration must not violate:

• engine statelessness
• engine isolation
• runtime orchestration rules

------------------------------------------------------------
SECTION 6 — EXPECTED OUTPUT
------------------------------------------------------------

Output must be a full architecture document:

WHYCESPACE WBSM v3 — GOVERNANCE INTEGRATION STANDARD

The document must contain:

• governance pipeline diagrams
• identity enforcement model
• policy evaluation architecture
• evidence anchoring architecture
• governance event types
• governance observability

The document must be written as a **LOCKED canonical architecture standard**.