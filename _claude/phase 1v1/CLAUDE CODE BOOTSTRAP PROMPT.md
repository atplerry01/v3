# WHYCESPACE WBSM v3
# CLAUDE CODE BOOTSTRAP PROMPT (MASTER IMPLEMENTATION RULES)

This file is the **master implementation contract** for Whycespace.

Claude Code MUST read this file before implementing **any phase**.

Its purpose is to guarantee:

• no architecture drift  
• deterministic code generation  
• correct repository placement  
• correct engine tiering  
• correct workflow orchestration  
• enterprise-grade code structure  

Violation of these rules is NOT allowed.

---

# 1 REPOSITORY STRUCTURE

All implementation MUST follow this repository structure.

```
whycespace/

src/
├── system/
│   ├── upstream/
│   ├── midstream/
│   └── downstream/
│
├── engines/
│
├── runtime/
│
├── domain/
│
├── platform/
│
└── shared/
```

Important rule:

```
src/system must ONLY contain:

upstream
midstream
downstream
```

No runtime code, domain code, or engines are allowed inside `system`.

---

# 2 SYSTEM ARCHITECTURE

Whycespace is organized into three system layers.

```
Upstream
Midstream
Downstream
```

Location:

```
src/system/
```

Structure:

```
src/system/
├── upstream/
├── midstream/
└── downstream/
```

---

## Upstream Systems

```
WhycePolicy
WhyceChain
WhyceID
```

Location:

```
src/system/upstream/
```

Purpose:

Governance, identity, and constitutional enforcement.

---

## Midstream Systems

```
HEOS
WSS
WhyceAtlas
WhycePlus
```

Location:

```
src/system/midstream/
```

Purpose:

Orchestration, economic coordination, intelligence, and system planning.

---

## Downstream Systems

```
Clusters
SPVs
Economic Systems
```

Location:

```
src/system/downstream/
```

Clusters represent economic sectors.

Example clusters used in implementation:

```
WhyceMobility → Taxi
WhyceProperty → PropertyLetting
```

---

# 3 ENGINE TAXONOMY

All deterministic processing occurs inside engines.

Location:

```
src/engines/
```

Structure:

```
src/engines/
├── T0U_Constitutional/
├── T1M_Orchestration/
├── T2E_Execution/
├── T3I_Intelligence/
└── T4A_Access/
```

Engine tiers:

```
T0U  Constitutional Engines
T1M  Orchestration Engines
T2E  Execution Engines
T3I  Intelligence Engines
T4A  Access Engines
```

Rules:

• Engines MUST be stateless  
• Engines cannot call other engines  
• Engines cannot access workflow state  
• Engines cannot mutate workflow directly  
• Engines only process context and return results  

All orchestration is controlled by the runtime.

---

# 4 ENGINE EXCHANGE CONTRACT

All engines MUST implement the following interface.

```csharp
public interface IEngine
{
    string Name { get; }

    Task<EngineResult> ExecuteAsync(EngineContext context);
}
```

Invocation envelope:

```
EngineInvocationEnvelope
```

Fields:

```
InvocationId
EngineName
WorkflowId
WorkflowStep
PartitionKey
Context
```

Engines return:

```
EngineResult
```

Containing:

```
Success
Events
Output
```

---

# 5 RUNTIME INFRASTRUCTURE

Runtime infrastructure manages orchestration and execution.

Location:

```
src/runtime/
```

Structure:

```
src/runtime/
├── dispatcher/
├── workflow/
├── events/
├── partitions/
├── projections/
├── reliability/
├── observability/
└── registry/
```

Responsibilities:

```
workflow orchestration
engine invocation
event streaming
projection updates
partition scheduling
runtime reliability
```

---

# 6 WORKFLOW SYSTEM (WSS)

Workflows are defined in the WSS system.

Location:

```
src/system/midstream/WSS/
```

Structure:

```
WSS/
├── contracts/
├── workflows/
├── mapping/
├── orchestration/
├── routing/
├── dispatcher/
├── execution/
├── kafka/
├── events/
├── observability/
└── configuration/
```

Rules:

Workflows define **graphs only**.

Workflows MUST NOT contain business logic.

Workflows define:

```
WorkflowGraph
WorkflowStep
```

Execution logic is delegated to engines.

---

# 7 COMMAND PATTERN

All state mutation follows the pattern:

```
UseCase
 ↓
Command
 ↓
Workflow
 ↓
Runtime Dispatcher
 ↓
Engine
 ↓
Event
```

Commands must be immutable records.

Example:

```csharp
public sealed record RequestRideCommand(
    Guid CommandId,
    Guid UserId,
    GeoLocation PickupLocation
);
```

---

# 8 GLOBAL EVENT FABRIC

The system uses Kafka for event streaming.

Topics:

```
whyce.commands
whyce.workflow.events
whyce.engine.events
whyce.cluster.events
whyce.spv.events
whyce.economic.events
whyce.system.events
```

Event schema:

```
eventId
eventType
aggregateId
timestamp
payload
```

---

# 9 PROJECTION ARCHITECTURE

Projections build read models.

Location:

```
src/runtime/projections/
```

Examples:

```
DriverLocationProjection
PropertyListingProjection
VaultBalanceProjection
RevenueProjection
```

Rules:

• projections subscribe to Kafka events  
• projections update Redis / Elastic / Postgres  
• projections are read-only  

---

# 10 REAL-TIME DECISION ENGINES

Decision engines must follow strict rules.

```
stateless
deterministic
read projections only
no direct database access
```

Examples:

```
DriverMatchingEngine
TenantMatchingEngine
WorkforceAssignmentEngine
```

Location:

```
src/engines/T3I_Intelligence/
```

---

# 11 CLUSTER ARCHITECTURE

Clusters represent economic sectors.

Structure:

```
Clusters
├── ClusterAdministration
├── ClusterProviders
└── SubClusters
```

Location:

```
src/system/downstream/clusters/
```

Clusters MUST NOT contain domain models.

Domain models belong in:

```
src/domain/
```

---

# 12 ECONOMIC SYSTEM

Economic lifecycle:

```
Vault
 ↓
Capital
 ↓
SPV
 ↓
Asset
 ↓
Revenue
 ↓
Profit Distribution
```

Domain location:

```
src/domain/economic/
```

Execution handled by:

```
T2E Execution Engines
```

---

# 13 RELIABILITY SYSTEM

Runtime reliability components:

```
WorkflowStateStore
IdempotencyRegistry
RetryPolicyEngine
TimeoutManager
SagaCoordinator
DeadLetterQueue
```

Storage:

```
Postgres → workflow state
Redis → active workflow cache
```

---

# 14 PLATFORM ACCESS

External systems interact through the platform layer.

Location:

```
src/platform/
```

Structure:

```
platform/
├── gateway/
│   └── WhyceApiGateway/
│
├── controlplane/
│   └── OperatorConsole/
│
└── ui/
    └── WhycePortal/
```

Components:

```
CommandController
QueryController
PolicyMiddleware
Authentication
```

Commands mutate state.

Queries read projections.

---

# 15 DETERMINISTIC CODE RULES

All code must follow deterministic architecture rules.

```
sealed classes
immutable records
no reflection
no dynamic runtime generation
no global mutable state
```

---

# 16 TEST REQUIREMENTS

Every component MUST include tests.

Structure:

```
tests/
├── engines/
├── workflows/
├── projections/
└── domain/
```

Test types:

```
unit tests
workflow tests
engine tests
projection tests
```

---

# 17 DEBUG API

Debug endpoints must exist for development.

Examples:

```
GET /dev/workflows
GET /dev/engines
GET /dev/projections
POST /dev/workflow/run
POST /dev/event/replay
```

---

# 18 IMPLEMENTATION OUTPUT FORMAT

Every phase implementation MUST return:

```
1 Files Created
2 Repo Tree
3 Build Result
4 Tests Result
5 Deterministic Validation
6 Debug Endpoints
```

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
28 passed
0 failed
```

---

# 19 ARCHITECTURE GUARDRAILS

The following violations are forbidden:

```
engines calling engines
workflows containing business logic
clusters containing domain models
stateful execution engines
direct database access from decision engines
```

---

# 20 DEVELOPMENT PHILOSOPHY

Whycespace must always remain:

```
deterministic
event-driven
policy-governed
cluster-scalable
```

All development MUST follow **WBSM v3 architecture**.

End of bootstrap.s