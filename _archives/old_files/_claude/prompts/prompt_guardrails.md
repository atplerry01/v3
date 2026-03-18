# WHYCESPACE WBSM v3
# PROMPT GUARDRAILS (CANONICAL)

This document defines the guardrails that prevent AI code generation from violating the Whycespace WBSM v3 architecture.

These rules MUST be applied to every code generation prompt.

If any rule is violated, the AI must STOP and request clarification before continuing.

---

# PRIMARY ARCHITECTURE PRINCIPLE

Whycespace follows strict layered architecture.

domain → business structure  
system → runtime models  
engines → runtime logic  
runtime → orchestration  
platform → access layer  
infrastructure → integrations

These layers must never be mixed.

---

# LAYER DEPENDENCY RULES

Allowed dependencies:

engines → domain  
engines → system  

runtime → engines  

platform → runtime  

infrastructure → external systems  

---

Forbidden dependencies:

domain → engines  
domain → runtime  
system → engines  
system → runtime  

clusters → other clusters  

---

# DOMAIN ISOLATION RULE

Each cluster is a bounded context.

Clusters must never depend on each other.

Shared logic must be placed inside:

src/domain/core/

Example violation:

WhyceMobility importing WhyceProperty.

Correct solution:

Move shared logic into:

src/domain/core/

---

# DOMAIN CONTENT RULE

The domain layer represents business concepts.

Allowed inside domain:

entities  
aggregates  
value objects  
domain services  
domain policies  

Forbidden inside domain:

engines  
stores  
workflow runtime  
database logic  
API controllers  

---

# SYSTEM LAYER RULE

The system layer contains runtime models.

Examples:

WorkflowState  
RetryDecision  
TimeoutEntry  
EventEnvelope  

System layer must not contain:

engines  
runtime logic  
database logic  

System models are shared across runtime engines.

---

# ENGINE RULES

All engines must follow the WBSM engine standard.

Engines must be:

stateless  
thread-safe  
deterministic  

Engines must not:

persist state  
contain domain entities  
execute external side effects  

Engines operate on models and return decisions.

---

# STORE RULE

Stores hold runtime state.

Stores must exist inside the engine layer.

Example:

src/engines/T1M/WSS/stores/

Stores may contain:

ConcurrentDictionary  
in-memory state  
runtime tracking  

Stores must not contain business logic.

---

# EVENT ARCHITECTURE RULE

Whycespace runtime is event-driven.

Engines do not call other engines directly.

Instead they emit events.

Example flow:

Engine
    ↓
Event Router
    ↓
Kafka Event Fabric
    ↓
Runtime Dispatcher
    ↓
Next Engine

Direct engine-to-engine calls are forbidden.

---

# WORKFLOW ORCHESTRATION RULE

WSS orchestrates workflow execution.

Execution flow:

WorkflowLifecycleEngine
        ↓
RuntimeDispatcher
        ↓
PartitionRouter
        ↓
WorkerPools
        ↓
ExecutionEngines

Lifecycle engine coordinates execution but never performs business logic.

---

# CLUSTER ECONOMY RULE

Clusters represent economic sectors.

Cluster hierarchy:

Cluster
    ↓
ClusterAdministration
    ↓
ClusterProviders
    ↓
SubClusters
    ↓
SPVs

SPVs are the legal economic operators.

Clusters must remain isolated bounded contexts.

---

# ENGINE INVOCATION RULE

Engines must never call other engines directly.

Engines must be invoked through the runtime orchestration layer.

Correct flow:

Runtime Dispatcher → Engine

Incorrect flow:

Engine → Engine

---

# FILE GENERATION RULE

Before generating any file, the AI must:

1 Print the target file path
2 Verify the path matches canonical architecture
3 Confirm correct layer placement

If any mismatch occurs:

STOP  
Ask for clarification

Do not guess the location.

---

# STRUCTURE VALIDATION

Before generating code:

Print the target file structure.

Verify it matches:

src/

domain/  
system/  
engines/  
runtime/  
platform/  
infrastructure/

If the structure differs:

Correct it before generating code.

---

# CODE GENERATION SAFETY

The AI must never:

create new top-level folders  
move existing layers  
mix domain and runtime logic  
create hidden dependencies  

When unsure:

STOP and ask the user.

---

# IMPLEMENTATION VALIDATION CHECKLIST

Before completing a task verify:

No engines inside src/system  
No models inside src/engines  
No cluster cross-dependencies  
All engines stateless  
All stores isolated  

---

# SUCCESS CRITERIA

Implementation is valid when:

Build succeeds  
0 warnings  
0 errors  
All tests pass  
Architecture remains unchanged

---

# STATUS

WBSM v3 PROMPT GUARDRAILS  
STATUS: CANONICAL