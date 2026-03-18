# WHYCESPACE — WBSM v3 FULL AUTO REFACTOR

You are operating under **WBSM v3 STRICT MODE**.

NON-NEGOTIABLE RULES:

1. NO DRIFT — All structures must match canonical architecture
2. NO DUPLICATION — One responsibility per module
3. STRICT LAYERING — Runtime ≠ Domain ≠ Infrastructure
4. ENGINES ARE STATELESS — No persistence, no side effects
5. EVENT-DRIVEN ONLY — No direct chaining
6. POLICY-GATED — Must support WHYCEPOLICY integration
7. BUILD MUST SUCCEED — Zero errors

---

# OBJECTIVE

Refactor the entire `src/runtime/` module to:

- Remove duplication
- Enforce canonical structure
- Align with WBSM v3
- Improve maintainability
- Preserve all functionality

---

# 🔴 STEP 1 — FIX ENGINE METADATA DUPLICATION

MERGE:

- engine-manifest/
- engine-registry/

INTO:

src/runtime/engine-metadata/

STRUCTURE:

engine-metadata/
 ├── manifest/
 ├── registry/
 ├── discovery/
 ├── validation/
 └── models/

TASKS:

- Move all files appropriately
- Remove duplicate models (EngineDescriptor, EngineTier, etc.)
- Ensure single source of truth
- Update all references/imports

---

# 🔴 STEP 2 — UNIFY COMMAND SYSTEM

CURRENT:

- command/
- command-registry/
- command-routing/

REFACTOR INTO:

src/runtime/command/

STRUCTURE:

command/
 ├── core/        (existing command/)
 ├── registry/
 ├── routing/

TASKS:

- Move files into new structure
- Fix namespaces
- Update all imports

---

# 🔴 STEP 3 — REMOVE EVENT ENVELOPE DUPLICATION (CRITICAL)

FOUND IN:

- event-fabric/models/EventEnvelope.cs
- event-fabric-runtime/models/EventEnvelope.cs

ACTION:

1. Create:

src/shared/contracts/events/EventEnvelope.cs

2. Move unified model there

3. Delete duplicates

4. Update ALL references across runtime

---

# 🔴 STEP 4 — FIX RELIABILITY SPLIT

KEEP BOTH MODULES BUT ENFORCE:

reliability/ → PURE LOGIC (engines, policies)
reliability-runtime/ → EXECUTION (timers, queues, workers)

TASKS:

- Move misplaced files accordingly
- Ensure no runtime logic in reliability/
- Ensure no business logic in runtime

---

# 🔴 STEP 5 — REBUILD SIMULATION ENGINE (MANDATORY)

DELETE CURRENT simulation/ STRUCTURE

RECREATE AS:

simulation/
 ├── engine/
 │   └── SimulationEngine.cs
 ├── models/
 │   ├── SimulationCommand.cs
 │   ├── SimulationContext.cs
 │   ├── SimulationExecutionResult.cs
 │   ├── SimulationEventRecord.cs
 │   ├── SimulationTrace.cs
 │   └── SimulationStateSnapshot.cs
 ├── policy/
 │   └── SimulationPolicy.cs
 ├── builder/
 │   └── SimulationEngineBuilder.cs
 └── exceptions/
     └── SimulationException.cs

REQUIREMENTS:

- Must simulate command execution
- Must NOT mutate state
- Must capture events instead of publishing
- Must integrate with RuntimeControlPlane
- Must produce SimulationExecutionResult

---

# 🔴 STEP 6 — MOVE PERSISTENCE TO INFRASTRUCTURE

CURRENT:

src/runtime/persistence/

ACTION:

1. Move implementations to:

infrastructure/persistence/

2. KEEP ONLY interfaces in runtime:

runtime/persistence/
 ├── contracts/
 ├── interfaces/

3. Update DI bindings

---

# 🔴 STEP 7 — ADD RUNTIME CONTEXT LAYER (MISSING)

CREATE:

src/runtime/runtime-context/

FILES:

- RuntimeContext.cs
- ExecutionContext.cs
- CorrelationContext.cs
- TenantContext.cs
- RequestContext.cs

PURPOSE:

- trace correlation
- multi-tenancy
- execution metadata

---

# 🔴 STEP 8 — ADD POLICY ENFORCEMENT MIDDLEWARE

CREATE:

src/runtime/policy-enforcement/

FILES:

- RuntimePolicyMiddleware.cs
- PolicyEvaluationPipeline.cs
- PolicyDecisionAdapter.cs

REQUIREMENTS:

- Intercepts all commands before execution
- Calls WHYCEPOLICY (mock interface)
- Blocks execution if denied

---

# 🔴 STEP 9 — ADD TRACE CORRELATION SYSTEM

CREATE:

src/runtime/trace-correlation/

FILES:

- CommandEventCorrelation.cs
- CorrelationRegistry.cs
- TraceLinker.cs

PURPOSE:

- Link command → engine → event
- Ensure full traceability

---

# 🔴 STEP 10 — ADD VERSIONING SYSTEM

CREATE:

src/runtime/versioning/

FILES:

- RuntimeVersionManager.cs
- CompatibilityMatrix.cs
- UpgradeCoordinator.cs

---

# 🔴 STEP 11 — ADD PROJECTION GOVERNANCE

CREATE:

src/runtime/projection-governance/

FILES:

- ProjectionPolicy.cs
- ProjectionConsistencyRules.cs
- ProjectionAccessControl.cs

---

# 🔴 STEP 12 — CLEAN DUPLICATES + DEAD FILES

- Remove duplicate models
- Remove unused files
- Ensure single responsibility per module

---

# 🔴 STEP 13 — VALIDATION

ENSURE:

- Solution builds successfully
- No broken references
- No circular dependencies
- All namespaces correct

---

# 🔴 STEP 14 — OUTPUT

Provide:

1. Updated folder structure
2. Summary of changes
3. List of files moved/removed
4. Confirmation of WBSM compliance

---

# FINAL RULE

If ANY structure violates WBSM v3:

→ FIX IT automatically  
→ DO NOT ASK QUESTIONS  

Proceed with FULL REFACTOR.