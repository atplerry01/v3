# WHYCESPACE WBSM v3
# MASTER AI ARCHITECTURE BOOTSTRAP PROMPT

This prompt MUST be executed before generating any code or architecture artifacts.

This ensures all AI-generated implementations comply with the canonical Whycespace architecture.

Failure to follow this prompt may cause architecture drift.

------------------------------------------------------------
SECTION 1 — SYSTEM CONTEXT
------------------------------------------------------------

You are working inside the **Whycespace WBSM v3 architecture**.

Whycespace is a **constitution-first distributed economic platform** built with:

• event-driven architecture  
• CQRS + event sourcing  
• stateless execution engines  
• workflow orchestration  
• policy-as-code governance  

The system uses **strict layered architecture**.

Canonical layers:

Constitutional Layer  
Economic Domain Layer  
Governance Layer  
Runtime Execution Layer  
Event Fabric Layer  
Reliability Layer  
Observability Layer  
Projection Layer  

All development must respect these layers.

------------------------------------------------------------
SECTION 2 — CANONICAL ARCHITECTURE DOCUMENTS
------------------------------------------------------------

Before generating any code you MUST load and respect the following documents:

1. architecture-lock.md  
2. implementation-guardrails.md  
3. runtime-execution-model.md  
4. workflow-system-standard.md  
5. engine-implementation-standard.md  
6. event-fabric-kafka-standard.md  
7. projection-read-model-standard.md  
8. cluster-runtime-standard.md  
9. prompt-generation-standard.md  

If a requested implementation conflicts with these documents:

YOU MUST STOP and request clarification.

You must NEVER invent architecture rules.

------------------------------------------------------------
SECTION 3 — REPOSITORY STRUCTURE
------------------------------------------------------------

The repository follows the canonical structure:

v3/

claude/  
docs/  
infrastructure/  
scripts/  
simulation/  

src/

domain/  
engines/  
runtime/  
platform/  
shared/  
system/  

tests/

.github/workflows/

You are NOT allowed to:

• create new top-level directories  
• move architecture layers  
• invent folder structures  

If a target file path violates the structure:

STOP and ask for clarification.

------------------------------------------------------------
SECTION 4 — LAYER DEPENDENCY RULES
------------------------------------------------------------

Allowed dependency direction:

domain  
↓  
system  
↓  
engines  
↓  
runtime  
↓  
platform  
↓  
infrastructure  

Forbidden dependencies:

domain → engines  
domain → runtime  
system → runtime  
system → engines  
cluster → cluster

Engines must never call other engines directly.

Communication must occur through events.

------------------------------------------------------------
SECTION 5 — ENGINE RULES
------------------------------------------------------------

Execution engines must follow strict constraints.

Engines must be:

• stateless  
• deterministic  
• thread-safe  

Engines must NOT:

• persist data  
• call other engines  
• access infrastructure directly  
• contain domain model definitions  

Execution contract:

EngineInput → EngineExecution → EngineResult → DomainEvent

Events are published through the runtime event fabric.

------------------------------------------------------------
SECTION 6 — RUNTIME EXECUTION MODEL
------------------------------------------------------------

All commands must pass through the runtime pipeline.

Canonical execution flow:

Command  
↓  
Workflow Creation  
↓  
Workflow Scheduler  
↓  
Runtime Dispatcher  
↓  
Engine Invocation  
↓  
Event Fabric (Kafka)  
↓  
Projection Workers  
↓  
Read Models  

No command may invoke engines directly.

------------------------------------------------------------
SECTION 7 — EVENT FABRIC RULES
------------------------------------------------------------

Events are the primary communication mechanism.

Event structure:

EventId  
EventType  
AggregateId  
SequenceNumber  
Payload  
Metadata  
Timestamp  
TraceId  
CorrelationId  
EventVersion  

Partition routing:

AggregateId → Kafka Key → Partition

Kafka provides distributed event transport.

Engines must never publish events directly.

All events go through EventPublisher.

------------------------------------------------------------
SECTION 8 — PROJECTION MODEL
------------------------------------------------------------

The system uses CQRS.

Write side:

Engines → Events

Read side:

Projections → Read Models

Projection workers subscribe to Kafka topics.

Projections must:

• be idempotent  
• never emit events  
• never invoke engines  

------------------------------------------------------------
SECTION 9 — WORKFLOW ORCHESTRATION
------------------------------------------------------------

WSS (Whyce Structural System) orchestrates execution.

Workflows are defined as DAG graphs.

Workflow structure:

WorkflowDefinition  
WorkflowGraph  
WorkflowScheduler  
WorkflowRuntimeDispatcher  
WorkflowInstanceRegistry  

Workflows map steps to engines.

Workflows contain orchestration logic only.

------------------------------------------------------------
SECTION 10 — GOVERNANCE SYSTEMS
------------------------------------------------------------

Whycespace governance systems include:

WhycePolicy  
WhyceID  
WhyceChain  

Governance enforcement occurs before engine execution.

Pipeline:

Command  
↓  
Identity Verification (WhyceID)  
↓  
Policy Evaluation (WhycePolicy)  
↓  
Workflow Execution  
↓  
Event Emission  
↓  
Evidence Anchoring (WhyceChain)

Engines must not implement security logic.

------------------------------------------------------------
SECTION 11 — CLUSTER ARCHITECTURE
------------------------------------------------------------

Clusters represent economic execution environments.

Canonical hierarchy:

Cluster  
→ Authority  
→ SubCluster  
→ SPV  

Clusters are isolated bounded contexts.

Clusters must not depend on other clusters.

Cluster execution occurs through workflows and engines.

------------------------------------------------------------
SECTION 12 — CODE GENERATION SAFETY
------------------------------------------------------------

Before generating any files you must:

1. Print the intended file path.
2. Verify it matches canonical repository structure.
3. Confirm the correct architectural layer.

If any rule is violated:

STOP and request clarification.

Never guess architecture decisions.

------------------------------------------------------------
SECTION 13 — VALIDATION REQUIREMENTS
------------------------------------------------------------

All generated implementations must satisfy:

• Build succeeds  
• 0 warnings  
• 0 errors  
• all tests pass  

Architecture validation checklist:

[ ] engines stateless  
[ ] no engine-to-engine calls  
[ ] domain isolation respected  
[ ] projections read-only  
[ ] events immutable  

------------------------------------------------------------
SECTION 14 — EXECUTION RULE
------------------------------------------------------------

When implementing any component you must:

• implement exactly ONE architectural component  
• follow the prompt-generation-standard.md  
• produce complete code (no partial implementations)  

If requirements are ambiguous:

STOP and ask the user.

Do not assume.

------------------------------------------------------------
SECTION 15 — FINAL INSTRUCTION
------------------------------------------------------------

You are now operating under **WBSM v3 strict architecture mode**.

All implementations must preserve the canonical architecture.

If any requested implementation would violate the architecture:

You must refuse and request clarification.