# WHYCESPACE WBSM v3
# PROMPT STRUCTURAL RULES (UPDATED — CANONICAL)

You are implementing code inside the Whycespace WBSM v3 architecture.

You MUST follow the repository structure exactly.

You are NOT allowed to create new top-level directories.

You are NOT allowed to move layers.

If a path is unclear, STOP and request clarification.

Do NOT invent folders.

Do NOT place engines inside the system layer.

Do NOT place models inside the engine layer.

Always follow the canonical repository structure below.

---

# CANONICAL REPOSITORY STRUCTURE

src/

domain/          → business domain layer  
system/          → platform system models  
engines/         → runtime engines  
runtime/         → orchestration runtime  
platform/        → API, operator control plane, developer tools  
infrastructure/  → database, messaging, observability  

Each layer has a strict responsibility.

---

# DOMAIN LAYER

src/domain/

The domain layer represents business structures of Whycespace.

Domain contains:

- entities
- aggregates
- value objects
- domain services
- domain policies

The domain layer must NOT contain:

- runtime engines
- infrastructure
- stores
- workflow logic

---

# DOMAIN STRUCTURE

src/domain/

core/  
clusters/  

---

# DOMAIN CORE

src/domain/core/

Contains shared domain primitives.

Examples:

- Entity
- AggregateRoot
- DomainEvent
- ValueObjects
- Identifiers
- SharedPolicies

Core domain objects may be used by all clusters.

---

# CLUSTER DOMAIN STRUCTURE

Clusters represent economic sectors.

src/domain/clusters/

Example clusters:

- WhyceMobility
- WhyceProperty
- WhyceEnergy
- WhyceAssets

Each cluster is a bounded context.

---

# CLUSTER INTERNAL STRUCTURE

Example:

src/domain/clusters/WhyceMobility/

ClusterAdministration/  
ClusterProviders/  
SubClusters/  
SPVs/  

Example SubCluster:

src/domain/clusters/WhyceMobility/SubClusters/Taxi/

Example Property cluster:

src/domain/clusters/WhyceProperty/SubClusters/Letting/

---

# CLUSTER ISOLATION RULE

Clusters must be independent bounded contexts.

Clusters must NOT depend on other clusters.

Shared logic must live inside:

src/domain/core/

---

# SYSTEM LAYER

src/system/

The system layer contains platform runtime models.

Examples:

- WorkflowState
- RetryDecision
- TimeoutEntry
- EventEnvelope
- EngineManifest

System layer contains ONLY:

- system models
- DTOs
- schemas
- contracts

System layer must NEVER contain:

- runtime engines
- stores
- execution logic

---

# ENGINE LAYER

src/engines/

Contains stateless runtime engines.

Engine tiers:

T0U → Constitutional Engines  
T1M → Orchestration Engines  
T2E → Execution Engines  
T3I → Intelligence Engines  
T4A → Access Engines  

Example:

src/engines/T1M/WSS/

---

# ENGINE RULES

All engines must be:

- stateless
- thread-safe
- deterministic

Engines must NOT:

- store persistent state
- contain domain definitions

Engines may depend on:

- domain
- system

But:

- domain must NOT depend on engines
- system must NOT depend on engines

---

# STORE RULE

Stores exist to maintain runtime state.

Stores must live inside the engine layer.

Example:

src/engines/T1M/WSS/stores/

Stores may contain:

- ConcurrentDictionary
- cache
- state tracking

Stores must NOT contain business logic.

---

# WSS IMPLEMENTATION RULE

WSS is the Whyce Structural System.

WSS is a T1M orchestration system.

---

# WSS MODEL LOCATION

src/system/midstream/WSS/models/

Examples:

- WorkflowState
- WorkflowFailurePolicy
- RetryDecision
- TimeoutEntry
- TimeoutDecision
- LifecycleDecision

---

# WSS ENGINE LOCATION

src/engines/T1M/WSS/

Example engines:

- WorkflowRegistry
- WorkflowGraphEngine
- WorkflowEventRouter
- WorkflowRetryPolicyEngine
- WorkflowTimeoutEngine
- WorkflowLifecycleEngine

---

# WSS STORE LOCATION

src/engines/T1M/WSS/stores/

Examples:

- WorkflowRetryStore
- WorkflowTimeoutStore
- WorkflowInstanceStore
- WorkflowStateStore

---

# IMPORT RULE

Engines may import system models.

Correct:

using Whycespace.System.Midstream.WSS.Models;

Incorrect:

system importing engines  
domain importing engines

---

# RUNTIME LAYER

src/runtime/

Contains orchestration runtime infrastructure.

Examples:

- RuntimeDispatcher
- PartitionRouter
- WorkerPools
- EventFabricConnector

Runtime layer is responsible for:

- engine invocation
- workflow scheduling
- distributed execution

---

# PLATFORM LAYER

src/platform/

Contains:

- API controllers
- operator control plane
- developer tools
- debug controllers

Examples:

- DebugController
- CommandController
- OperatorController

---

# INFRASTRUCTURE LAYER

src/infrastructure/

Contains integrations:

- Postgres
- Redis
- Kafka
- Monitoring
- Logging

Examples:

- KafkaEventPublisher
- DatabaseRepositories
- Telemetry

---

# FILE PLACEMENT RULE

Before generating any file, verify:

1. File path matches canonical architecture  
2. Layer placement is correct  
3. No new folder hierarchy is invented  

If the correct path does not exist, create it exactly as defined.

---

# ENGINE IMPLEMENTATION RULES

All engines must be:

- stateless
- thread-safe
- deterministic

Engines must never call other engines directly.

Engines must be invoked by the runtime layer.

---

# VALIDATION RULE

Before completing implementation confirm:

- No engine created inside src/system/
- No models created inside src/engines/
- Domain layer respected
- Cluster isolation respected

---

# STRUCTURE VALIDATION

Before generating code:

Print the target file structure.

Verify the structure matches the canonical repository.

If structure does not match:

Correct it before writing code.

---

# SUCCESS CRITERIA

Implementation is successful when:

- Build succeeds
- 0 warnings
- 0 errors
- All tests pass

---

# STATUS

WBSM v3 PROMPT STRUCTURE  
STATUS: CANONICAL