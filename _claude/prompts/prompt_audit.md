# WHYCESPACE WBSM v3
# FULL REPOSITORY ARCHITECTURE AUDIT PROMPT
# CANONICAL REPO VALIDATION

You are performing a full architecture audit of the Whycespace repository.

This is an inspection task only.

You must NOT modify files.
You must NOT generate replacement code.
You must NOT move files.
You must ONLY inspect the repository and produce a violation report.

Your task is to review the entire project against the canonical repository structure and architecture guardrails.

---

# AUDIT MODE

This is a READ-ONLY architecture audit.

You must:

1. inspect the full repository
2. compare the actual structure against the canonical locked repo structure
3. identify files and folders that violate the architecture rules
4. identify misplaced components
5. identify dependency direction violations
6. identify layer mixing
7. identify cluster cross-dependencies
8. identify direct engine-to-engine invocation violations
9. produce a structured report only

Do NOT fix anything yet.

---

# CANONICAL ROOT STRUCTURE

The standard repository root is:

/
├── claude/
├── docs/
├── infrastructure/
├── scripts/
├── simulation/
├── src/
└── tests/

Any root-level folder outside this standard must be reported unless it is clearly build/tooling metadata already expected by .NET or Git.

---

# CANONICAL SRC STRUCTURE

src/
├── domain/
├── engines/
├── platform/
├── runtime/
├── shared/
└── system/

This is the canonical application source structure.

No architecture layer should be created outside these canonical src folders unless explicitly locked elsewhere.

---

# CANONICAL DOMAIN STRUCTURE

src/domain/
├── application/
├── clusters/
├── core/
├── events/
├── shared/
├── bin/
└── obj/

Rules:

- domain is the business/domain layer
- domain may contain business models, aggregates, entities, value objects, domain services, domain policies, domain events, application-layer domain orchestration
- domain must NOT contain runtime engines
- domain must NOT contain platform controllers
- domain must NOT contain infrastructure integrations
- domain must NOT contain engine stores
- domain must NOT contain runtime dispatcher logic

Important:
- bin/ and obj/ are build artifacts and must be ignored during architecture violation reporting
- application/ is valid and canonical under domain
- shared/ under domain is valid and canonical
- events/ under domain is valid and canonical

---

# CANONICAL ENGINES STRUCTURE

src/engines/
├── T0U/
├── T1M/
├── T2E/
├── T3I/
├── T4A/
├── bin/
└── obj/

Rules:

- engines contain engine implementations
- engines are tiered by canonical engine taxonomy
- engines must remain stateless
- engines may depend on domain, system, and shared where appropriate
- engines must NOT define business/domain ownership structures
- engines must NOT contain platform controllers
- engines must NOT contain infrastructure persistence implementations unless explicitly engine-local runtime store abstractions are allowed by doctrine
- bin/ and obj/ must be ignored

Engine taxonomy:

- T0U = Constitutional Engines
- T1M = Orchestration Engines
- T2E = Execution Engines
- T3I = Intelligence Engines
- T4A = Access Engines

---

# CANONICAL PLATFORM STRUCTURE

src/platform/
├── cluster-templates/
├── controlplane/
├── gateway/
├── runtimeclient/
├── ui/
├── bin/
└── obj/

Rules:

- platform contains user/operator/API-facing surfaces
- controlplane, gateway, ui, runtimeclient are valid
- cluster-templates is valid
- platform must NOT contain engine implementations
- platform must NOT contain domain aggregates
- bin/ and obj/ must be ignored

---

# CANONICAL RUNTIME STRUCTURE

src/runtime/
├── command/
├── dispatcher/
├── engine/
├── engine-dispatch/
├── engine-manifest/
├── engine-workers/
├── event-fabric/
├── event-fabric-runtime/
├── event-idempotency/
├── event-replay/
├── event-schema/
├── events/
├── guardrails/
├── observability/
├── partition/
├── partitions/
├── persistence/
├── projection-rebuild/
├── projection-runtime/
├── projections/
├── registry/
├── reliability/
├── reliability-runtime/
├── simulation/
├── validation/
├── worker-pool/
├── workflow/
├── workflow-runtime/
├── bin/
└── obj/

Rules:

- runtime contains orchestration and execution-runtime infrastructure
- runtime dispatcher, partitions, worker pools, event fabric, reliability, projections, workflow runtime are all valid here
- runtime must NOT contain business domain ownership structures
- runtime must NOT contain cluster business models except runtime-specific projection/contract models if architecturally appropriate
- bin/ and obj/ must be ignored

---

# CANONICAL SHARED STRUCTURE

src/shared/
├── Contracts/
├── Identity/
├── Location/
├── Projections/
├── bin/
└── obj/

Rules:

- shared contains reusable cross-layer shared modules
- shared is allowed to hold contracts, identity primitives, location primitives, shared projections
- shared must NOT contain business cluster implementations
- shared must NOT contain runtime dispatcher logic
- shared must NOT contain platform controllers
- bin/ and obj/ must be ignored

---

# CANONICAL SYSTEM STRUCTURE

src/system/
├── upstream/
├── midstream/
├── downstream/
├── bin/
└── obj/

Rules:

- system contains system-layer structures organized by upstream, midstream, downstream
- system may contain models, contracts, schemas, DTOs, system-specific structures
- system must NOT contain platform controllers
- system must NOT contain runtime dispatcher infrastructure
- system must NOT contain build-time drift folders beyond expected bin/obj
- bin/ and obj/ must be ignored

---

# GENERAL AUDIT IGNORE RULE

Ignore these as violations unless they contain suspicious handwritten source code that appears wrongly placed:

- bin/
- obj/

These folders are build artifacts and are not architecture violations by default.

You may mention them in the report only under an "Ignored Build Artifacts" section if useful, but do not count them as violations.

---

# ARCHITECTURE RULES TO ENFORCE

## 1. Layer Placement Rules

Check for files/folders placed in the wrong canonical layer.

Examples of violations:

- engine implementation inside src/system/
- controller inside src/domain/
- cluster business model inside src/runtime/
- infrastructure adapter inside src/platform/ when it belongs in infrastructure/
- business cluster implementation inside src/shared/

---

## 2. Dependency Direction Rules

Allowed high-level dependency directions:

- domain may depend on domain/core/shared-domain primitives as permitted within the domain layer
- engines may depend on domain
- engines may depend on system
- engines may depend on shared
- runtime may depend on engines
- runtime may depend on shared
- platform may depend on runtime
- platform may depend on shared
- infrastructure may depend on external systems and support runtime/platform/system needs where appropriate

Forbidden dependency directions to report:

- domain importing engines
- domain importing runtime
- domain importing platform
- system importing platform runtime behavior incorrectly
- shared importing cluster-specific business domains
- cluster importing another cluster directly unless explicitly locked as allowed
- engines directly invoking other engines in a way that bypasses runtime orchestration
- platform embedding engine/business logic directly when it should delegate

---

## 3. Cluster Isolation Rules

Under:

src/domain/clusters/

Each cluster is a bounded context.

Report violations where one cluster directly depends on another cluster’s internal implementation.

Shared behavior should be placed in:

- src/domain/core/
- src/domain/shared/
- src/shared/

depending on actual architectural responsibility.

---

## 4. Engine Invocation Rules

Engines must not be tightly coupled through direct engine-to-engine orchestration calls if runtime orchestration is required by doctrine.

Report any likely direct engine invocation patterns that violate WBSM runtime orchestration.

Examples to flag:

- one engine constructing another engine directly
- one engine calling another engine as the orchestration mechanism
- lifecycle/orchestration logic embedded entirely inside engine-to-engine chains instead of runtime/dispatcher patterns

Do NOT flag normal dependency injection of interfaces automatically unless it clearly violates orchestration doctrine.
Use judgment and report suspicious cases separately as:
- confirmed violation
- likely violation requiring review

---

## 5. Store Placement Rules

Stores should exist only where permitted by the architecture.

If a store exists:

- inside engine layer runtime state storage → usually valid
- inside domain layer → violation
- inside platform layer → likely violation
- inside runtime only if clearly runtime infrastructure persistence support → review carefully
- inside shared only if truly generic contract store abstraction and not business/runtime state → review carefully

Report all suspicious store placements.

---

## 6. Runtime Content Rules

Runtime should contain:

- dispatcher
- worker pools
- partitions
- event fabric
- idempotency
- replay
- reliability
- workflow runtime
- validation
- registry
- observability
- projection runtime

Runtime should NOT contain:

- cluster business entities
- SPV business ownership models
- platform controllers
- UI components

---

## 7. Platform Content Rules

Platform should contain:

- gateway
- controlplane
- runtimeclient
- ui
- cluster-templates

Report violations if platform contains:

- engine implementations
- cluster domain aggregates
- runtime dispatcher internals
- business persistence logic that belongs elsewhere

---

## 8. Shared Content Rules

Shared should contain generic reusable modules only.

Report violations if shared contains:

- cluster-specific code
- platform controllers
- runtime orchestration
- engine implementations
- business bounded-context implementations

---

## 9. System Content Rules

System should contain upstream/midstream/downstream system structures.

Report violations if system contains:

- engine implementations
- runtime dispatcher/worker/partition logic
- UI/platform controllers
- cluster domain ownership logic that belongs in domain

---

# AUDIT PROCESS

You must inspect:

1. root folder structure
2. src folder structure
3. major folder placement
4. suspicious files in each layer
5. import/dependency direction
6. direct engine-to-engine orchestration violations
7. cluster cross-dependencies
8. misplaced stores
9. runtime/platform/shared/system misuse

Do a thorough inspection.

---

# OUTPUT FORMAT

Produce the report in exactly this structure:

# WHYCESPACE ARCHITECTURE AUDIT REPORT

## A. Canonical Structure Check
- list actual root folders
- list actual src folders
- identify structure mismatches

## B. Ignored Build Artifacts
- list bin/obj folders ignored from violation counting

## C. Layer Placement Violations
- list each violating file/folder
- explain why it violates the canonical repo structure

## D. Dependency Direction Violations
- list forbidden dependency directions found
- explain why each is invalid

## E. Cluster Isolation Violations
- list cross-cluster dependencies
- classify severity

## F. Engine Invocation Violations
- list confirmed and likely engine-to-engine orchestration violations

## G. Store Placement Violations
- list suspicious or invalid store locations

## H. Runtime / Platform / Shared / System Misplacement
- list misplaced content by layer

## I. Severity Summary
Classify each issue as:
- CRITICAL
- MAJOR
- MINOR

## J. Final Totals
Provide:
- total folders reviewed
- total files reviewed
- total violations
- critical count
- major count
- minor count

## K. Recommended Fix Groups
Do NOT write the fixes.
Only group violations into fix batches such as:
- Batch 1: folder relocation
- Batch 2: dependency cleanup
- Batch 3: cluster isolation cleanup
- Batch 4: engine orchestration cleanup

---

# IMPORTANT AUDIT BEHAVIOR

- Do NOT change any code
- Do NOT propose code edits yet
- Do NOT rewrite files
- Do NOT invent missing rules
- Use the canonical repo structure in this prompt as the source of truth
- If a case is ambiguous, mark it clearly as:
  - REVIEW REQUIRED

---

# SUCCESS CONDITION

A successful audit is a complete, structured, reviewable violation report covering the entire repository with no code changes.