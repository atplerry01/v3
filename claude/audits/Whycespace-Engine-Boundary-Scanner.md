You are performing a **full architecture compliance audit** for the Whycespace repository.

The system is operating under **Whycespace Build Strict Mode v3 (WBSM v3)**.

Your task is to scan the repository and detect **engine boundary violations**.

You MUST NOT modify the code.

You will only analyze and produce a **structured architecture report and refactoring plan**.

--------------------------------------------------

WHYCESPACE ENGINE ARCHITECTURE

The repository uses a strict **five-tier engine architecture**.

src/engines/

T0U  → Constitutional Engines
T1M  → Orchestration Engines
T2E  → Economic Execution Engines
T3I  → Intelligence Engines
T4A  → Access Engines

Engines must follow strict rules.

--------------------------------------------------

ENGINE RULES (MANDATORY)

Engines must be:

• stateless
• deterministic
• pure computation

Engines MUST NOT:

• persist state
• access databases
• contain repositories
• contain stores
• contain registries
• perform infrastructure logic
• call other engines directly

Engines may only contain:

• engine implementations
• command contracts
• result models
• mapping logic
• validation logic

--------------------------------------------------

VALID LOCATIONS FOR STATEFUL COMPONENTS

Registries must live in:

src/systems/

Stores must live in:

src/runtime/persistence/

Dispatchers must live in:

src/runtime/dispatcher/

Workers must live in:

src/runtime/engine-workers/

Projections must live in:

src/runtime/projection-runtime/

Domain models must live in:

src/domain/

--------------------------------------------------

REPOSITORY TO ANALYZE

Scan the entire repository starting from:

src/

Focus especially on:

src/engines/
src/runtime/
src/systems/
src/domain/

--------------------------------------------------

DETECTION TASKS

Identify and report the following violations:

1. STORES INSIDE ENGINES

Example violation:

src/engines/.../stores/

or any class named:

*Store
*Repository
*DbContext

inside engines.

--------------------------------------------------

2. REGISTRIES INSIDE ENGINES

Example violation:

WorkflowRegistry
GuardianRegistry
CapitalRegistry

inside src/engines/.

--------------------------------------------------

3. ENGINE-TO-ENGINE DEPENDENCIES

Detect when an engine references another engine.

Example violation:

using Whycespace.Engines.*

Engines must not call each other directly.

--------------------------------------------------

4. RUNTIME LOGIC INSIDE ENGINES

Examples:

dispatchers
worker loops
retry policies
timeouts
message routing

These belong in runtime/.

--------------------------------------------------

5. DOMAIN MODELS INSIDE ENGINES

Detect if engines define:

entities
aggregates
domain models

These must be inside:

src/domain/.

--------------------------------------------------

6. SYSTEM LOGIC INSIDE ENGINES

Examples:

Cluster creation
SPV creation
Participant registry
Workflow registry

These belong in:

src/systems/.

--------------------------------------------------

7. INFRASTRUCTURE DEPENDENCIES

Detect if engines reference:

Entity Framework
Dapper
Kafka clients
Redis clients
database drivers

These must not appear inside engines.

--------------------------------------------------

OUTPUT FORMAT

Produce a structured architecture report with the following sections.

--------------------------------------------------

SECTION 1 — ENGINE TIER OVERVIEW

List all engine tiers and their current folders.

Example:

T0U
T1M
T2E
T3I
T4A

--------------------------------------------------

SECTION 2 — VIOLATIONS DETECTED

For each violation provide:

Violation Type
File Path
Explanation
Severity

Severity levels:

Critical
High
Medium
Low

--------------------------------------------------

SECTION 3 — FILES THAT MUST MOVE

Provide a migration table.

Example:

Current Location
Correct Location
Reason

--------------------------------------------------

SECTION 4 — NAMESPACE VIOLATIONS

List namespaces that violate architecture boundaries.

Example:

Whycespace.Engines referencing runtime.

--------------------------------------------------

SECTION 5 — ENGINE PURITY CHECK

For each engine report:

Stateless ✓/✗
Persistence ✓/✗
Registry usage ✓/✗
Infrastructure usage ✓/✗

--------------------------------------------------

SECTION 6 — SAFE MIGRATION PLAN

Generate a safe migration plan including:

files to move
folders to create
namespace changes
dependency updates

Do NOT change code behavior.

--------------------------------------------------

SECTION 7 — FINAL ARCHITECTURE SCORE

Provide an architecture score out of 100.

Score categories:

Engine purity
Layer separation
Runtime isolation
System separation
Dependency hygiene

--------------------------------------------------

IMPORTANT

Do NOT modify code.

Only produce the audit report and refactoring plan.