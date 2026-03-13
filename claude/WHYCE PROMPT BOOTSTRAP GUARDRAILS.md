# WHYCESPACE WBSM v3
# AI IMPLEMENTATION BOOTSTRAP

You are implementing code inside the **Whycespace WBSM v3 architecture**.

Before generating any code you MUST read and enforce the following architectural guardrails.

If any rule cannot be satisfied you must STOP and request clarification.

You must never guess.

---

# SECTION 1 — PRIMARY ARCHITECTURE PRINCIPLE

Whycespace follows strict layered architecture.

domain → business structures  
system → runtime models  
engines → runtime logic  
runtime → orchestration layer  
platform → access layer  
infrastructure → external integrations  

These layers must never be mixed.

---

# SECTION 2 — CANONICAL REPOSITORY STRUCTURE

You MUST follow this repository structure exactly.

```
v3/

_claude/
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
```

You are NOT allowed to:

• create new top-level folders  
• move architectural layers  
• invent directory structures  

If a path is unclear → STOP and ask.

---

# SECTION 3 — LAYER DEPENDENCY RULES

Allowed dependencies:

```
engines → domain
engines → system

runtime → engines

platform → runtime

infrastructure → external systems
```

Forbidden dependencies:

```
domain → engines
domain → runtime
system → engines
system → runtime
clusters → other clusters
```

---

# SECTION 4 — DOMAIN LAYER RULES

Location:

```
src/domain/
```

Domain contains business structures only.

Allowed:

• entities  
• aggregates  
• value objects  
• domain services  
• domain policies  

Forbidden:

• runtime engines  
• database logic  
• stores  
• workflow logic  
• controllers  

Domain must remain **pure business logic**.

---

# SECTION 5 — DOMAIN STRUCTURE

```
src/domain/

core/
clusters/
application/
events/
shared/
```

Shared primitives must exist inside:

```
src/domain/core/
```

Example shared primitives:

• Entity  
• AggregateRoot  
• DomainEvent  
• ValueObject  
• Identifiers  

---

# SECTION 6 — CLUSTER DOMAIN STRUCTURE

Clusters represent economic sectors.

```
src/domain/clusters/
```

Example clusters:

```
WhyceMobility
WhyceProperty
WhyceEnergy
WhyceAssets
```

Cluster internal structure:

```
ClusterAdministration
ClusterProviders
SubClusters
SPVs
```

Clusters must remain **bounded contexts**.

Clusters must NEVER depend on other clusters.

---

# SECTION 7 — SYSTEM LAYER RULE

Location:

```
src/system/
```

System layer contains runtime models.

Examples:

• WorkflowState  
• RetryDecision  
• TimeoutEntry  
• EventEnvelope  
• EngineManifest  

System layer must NEVER contain:

• runtime engines  
• business logic  
• database logic  

System models are shared by engines and runtime.

---

# SECTION 8 — ENGINE LAYER

Location:

```
src/engines/
```

Engine tiers:

```
T0U → Constitutional Engines
T1M → Orchestration Engines
T2E → Execution Engines
T3I → Intelligence Engines
T4A → Access Engines
```

Example:

```
src/engines/T1M/WSS/
```

---

# SECTION 9 — ENGINE RULES

All engines must be:

• stateless  
• thread-safe  
• deterministic  

Engines must NOT:

• persist state  
• call other engines  
• contain domain definitions  

Engines may depend on:

```
domain
system
```

But:

```
domain must NOT depend on engines
system must NOT depend on engines
```

---

# SECTION 10 — STORE RULE

Stores maintain runtime state.

Stores must exist inside the engine layer.

Example:

```
src/engines/T1M/WSS/stores/
```

Stores may contain:

• ConcurrentDictionary  
• runtime tracking  
• in-memory cache  

Stores must NOT contain business logic.

---

# SECTION 11 — EVENT ARCHITECTURE

Whycespace runtime is event-driven.

Correct flow:

```
Engine
   ↓
Event Router
   ↓
Kafka Event Fabric
   ↓
Runtime Dispatcher
   ↓
Next Engine
```

Forbidden:

```
Engine → Engine
```

---

# SECTION 12 — ENGINE INVOCATION RULE

Engines must only be invoked by runtime.

Correct:

```
Runtime Dispatcher → Engine
```

Incorrect:

```
Engine → Engine
```

---

# SECTION 13 — WORKFLOW ORCHESTRATION

WSS orchestrates workflow execution.

Execution flow:

```
WorkflowLifecycleEngine
        ↓
RuntimeDispatcher
        ↓
PartitionRouter
        ↓
WorkerPools
        ↓
ExecutionEngines
```

WSS coordinates execution but never performs business logic.

---

# SECTION 14 — RUNTIME LAYER

Location:

```
src/runtime/
```

Runtime responsibilities:

• engine invocation  
• workflow scheduling  
• distributed execution  
• event routing  
• partition routing  

Examples:

```
RuntimeDispatcher
PartitionRouter
WorkerPools
EventFabricConnector
```

---

# SECTION 15 — PLATFORM LAYER

Location:

```
src/platform/
```

Platform provides system access.

Examples:

• API controllers  
• operator control plane  
• developer tools  

Example controllers:

```
DebugController
CommandController
OperatorController
```

---

# SECTION 16 — INFRASTRUCTURE LAYER

Location:

```
src/infrastructure/
```

Infrastructure contains integrations.

Examples:

• Postgres  
• Redis  
• Kafka  
• Monitoring  
• Logging  

---

# SECTION 17 — FILE GENERATION RULE

Before generating ANY file you must:

1 Print the target file path  
2 Validate the path matches canonical architecture  
3 Confirm correct layer placement  

If mismatch occurs:

STOP and ask the user.

---

# SECTION 18 — STRUCTURE VALIDATION

Before generating code print the target structure.

Verify it matches:

```
src/

domain/
system/
engines/
runtime/
platform/
shared/
```

If structure differs:

Correct it before generating code.

---

# SECTION 19 — CODE GENERATION SAFETY

You must never:

• create hidden dependencies  
• mix runtime and domain logic  
• create cross-cluster imports  
• place engines in the wrong layer  

If uncertain:

STOP and request clarification.

---

# SECTION 20 — IMPLEMENTATION VALIDATION CHECKLIST

Before finishing implementation verify:

• no engines inside src/system  
• no models inside src/engines  
• cluster isolation respected  
• engines stateless  
• stores isolated  

---

# SECTION 21 — SUCCESS CRITERIA

Implementation is valid when:

• Build succeeds  
• 0 warnings  
• 0 errors  
• All tests pass  
• Architecture remains unchanged

---

# STATUS

WBSM v3 AI PROMPT BOOTSTRAP  
STATUS: CANONICAL