```markdown
# WHYCESPACE WBSM v3
# MASTER PROMPT GENERATION TEMPLATE
# PHASE 2 IMPLEMENTATION

This template MUST be used to generate all implementation prompts.

This template prevents architecture drift and ensures all prompts follow the canonical Whycespace WBSM v3 architecture.

This template applies to the following component categories:

• Execution Engines  
• Intelligence Engines  
• Orchestration Engines  
• Runtime Components  
• Projection Systems  
• Data Stores  
• Workflow Engines  
• Governance Adapters  

No prompt may be generated without following this template.

------------------------------------------------------------
SECTION 1 — BOOTSTRAP EXECUTION
------------------------------------------------------------

Before generating any prompt you MUST load the architecture bootstrap prompt.

Required document:

claude/bootstrap-prompts/master-bootstrap.prompt.md

This document defines the canonical Whycespace architecture.

You must also load the following architecture standards:

claude/bootstrap-prompts/architecture-lock.md
claude/bootstrap-prompts/runtime-execution-model.md
claude/bootstrap-prompts/implementation-guardrails.md
claude/bootstrap-prompts/engine-implementation-standard.md
claude/bootstrap-prompts/event-fabric-kafka-standard.md
claude/bootstrap-prompts/projection-read-model-standard.md
claude/bootstrap-prompts/workflow-system-standard.md
claude/bootstrap-prompts/cluster-runtime-standard.md
claude/bootstrap-prompts/event-store-persistence-standard.md
claude/bootstrap-prompts/governance-integration-standard.md
claude/bootstrap-prompts/prompt-generation-standard.md

If any implementation violates these documents:

STOP and request clarification.

Do not guess architecture decisions.

------------------------------------------------------------
SECTION 2 — COMPONENT IDENTIFICATION
------------------------------------------------------------

Each prompt must implement EXACTLY ONE architectural component.

You must specify:

Component Number
Component Name
System Category
Architecture Layer
Engine Tier (if applicable)

Example:

Component Number:
2.0.1

Component Name:
Identity Registry

System Category:
WhyceID System

Architecture Layer:
System Layer

Engine Tier:
N/A

------------------------------------------------------------
SECTION 3 — REPOSITORY LOCATION VERIFICATION
------------------------------------------------------------

Before writing any prompt you must verify the correct repository location.

Canonical repository layers:

src/domain
src/system
src/engines
src/runtime
src/platform
infrastructure

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

If the component path violates this rule:

STOP and ask for clarification.

------------------------------------------------------------
SECTION 4 — COMPONENT PATH DECLARATION
------------------------------------------------------------

Every prompt must explicitly declare the intended file paths.

Example:

Target Implementation Path:

src/system/upstream/WhyceID/registry/

Associated Engine Path:

src/engines/T0U/WhyceID/

Associated Tests:

tests/whyceid-identity/

Claude Code must verify that these directories exist.

If a path does not exist:

Claude Code may create the directory only if it matches the canonical architecture.

------------------------------------------------------------
SECTION 5 — IMPLEMENTATION CATEGORY
------------------------------------------------------------

The prompt must specify the component category.

Allowed categories:

DOMAIN COMPONENT
SYSTEM MODEL
EXECUTION ENGINE
INTELLIGENCE ENGINE
ORCHESTRATION ENGINE
WORKFLOW ENGINE
RUNTIME COMPONENT
PROJECTION
STORE
GOVERNANCE ADAPTER

Each category has different implementation constraints.

------------------------------------------------------------
SECTION 6 — ENGINE RULES (IF APPLICABLE)
------------------------------------------------------------

If the component is an engine:

The engine must be:

• stateless  
• deterministic  
• thread-safe  

Engines must NOT:

• persist data  
• call other engines  
• access infrastructure directly  
• define domain models  

Execution contract:

EngineInput
→ EngineExecution
→ EngineResult
→ DomainEvent

Engines must emit events through the runtime event publisher.

------------------------------------------------------------
SECTION 7 — RUNTIME RULES
------------------------------------------------------------

Runtime components belong under:

src/runtime/

Runtime components may include:

command routing
engine dispatch
event fabric
worker pools
partition routing
projection runtime
reliability systems

Runtime components must NOT contain domain logic.

------------------------------------------------------------
SECTION 8 — PROJECTION RULES
------------------------------------------------------------

Projection systems must follow CQRS read-side architecture.

Projection responsibilities:

• consume Kafka events  
• build read models  
• support deterministic replay  

Projections must:

• be idempotent  
• never emit events  
• never invoke engines  

Projection location:

src/runtime/projection-runtime/
src/runtime/projections/

------------------------------------------------------------
SECTION 9 — STORE RULES
------------------------------------------------------------

Stores represent persistence adapters.

Stores may interact with:

PostgreSQL
Redis
Event Store

Stores must NOT contain business logic.

Stores must reside under:

src/system/**/stores/
or

src/runtime/**/stores/

------------------------------------------------------------
SECTION 10 — GOVERNANCE ADAPTER RULES
------------------------------------------------------------

Governance adapters enforce:

WhycePolicy
WhyceID
WhyceChain

Adapters must not contain business logic.

Adapters perform integration with governance systems.

------------------------------------------------------------
SECTION 11 — TEST REQUIREMENTS
------------------------------------------------------------

Each prompt must generate tests.

Tests must validate:

• deterministic behavior
• concurrency safety
• idempotency
• architecture compliance

All builds must pass:

0 errors  
0 warnings

------------------------------------------------------------
SECTION 12 — DEBUG ENDPOINTS
------------------------------------------------------------

Prompts must create developer debug endpoints.

Example:

/dev/identity/{id}

/dev/policy/evaluate

/dev/chain/verify

These endpoints must only exist in development builds.

------------------------------------------------------------
SECTION 13 — PROMPT STORAGE INSTRUCTIONS
------------------------------------------------------------

After generating the prompt, Claude Code MUST store the prompt file.

Storage location:

claude/phase2x/prompts/

File naming format:

<ComponentNumber>-<component-name>.prompt.md

Example:

2.0.1-identity-registry.prompt.md
2.0.2-identity-core-model.prompt.md
2.0.3-identity-attribute-engine.prompt.md

Rules:

• filenames MUST start with the component number
• filenames MUST be lowercase
• words separated by hyphen

Claude Code must create the directory if missing:

claude/phase2x/prompts/

------------------------------------------------------------
SECTION 14 — PROMPT OUTPUT FORMAT
------------------------------------------------------------

The generated prompt must contain:

Component Header
Architecture Verification
Target File Paths
Implementation Instructions
Testing Requirements
Validation Checklist

No partial implementations allowed.

------------------------------------------------------------
SECTION 15 — VALIDATION CHECKLIST
------------------------------------------------------------

Before completing the prompt generation, verify:

[ ] repository path valid  
[ ] architecture layer correct  
[ ] no forbidden dependencies  
[ ] engine rules respected  
[ ] prompt stored correctly  
[ ] test coverage defined  

------------------------------------------------------------
SECTION 16 — FINAL RULE
------------------------------------------------------------

You are operating under Whycespace WBSM v3.

All prompts generated with this template must preserve the canonical architecture.

If a requested implementation violates architecture rules:

STOP and request clarification.
```