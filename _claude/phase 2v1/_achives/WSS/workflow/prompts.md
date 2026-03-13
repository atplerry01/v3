Excellent — this is the **correct transition point** in the architecture.

You have now completed the **Tier-0 Upstream Constitutional Systems**:

```
WHYCEPOLICY
WhyceID
WhyceChain
Guardian Governance
```

Now we move to **Midstream Orchestration**, starting with the **WSS — Workflow Structural System**.

This system is **extremely important** because it is the **structural orchestration layer** between:

```
Platform / APIs (T4A)
        ↓
WSS (Workflow Structural System)
        ↓
T1M / T2E Engines
        ↓
Evidence → WhyceChain
Policy → WHYCEPOLICY
Identity → WhyceID
```

So WSS becomes the **execution brain of Whycespace**.

All prompts below follow your **locked Prompt Rule**:

* deterministic
* full prompt
* entire prompt inside **markdown container**

---

# MIDSTREAM SYSTEM

# WSS — Workflow Structural System

---

# PHASE 2.1.1 — Workflow Definition Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.1 — WORKFLOW DEFINITION ENGINE

You are implementing Phase 2.1.1 of the Whycespace system.

This phase introduces the Workflow Definition Engine.

The WSS (Workflow Structural System) defines how multi-step
system processes are structured and executed across engines.

A workflow definition describes the structure of a process
before any runtime instances exist.

Examples of workflows:

Taxi Ride Request
Property Letting Onboarding
SPV Capital Contribution
Policy Amendment Approval

---

# ARCHITECTURE RULES

WSS is a Midstream orchestration system.

Engines remain stateless.

Workflow definitions are immutable structures.

Workflow execution state will exist in later phases.

---

# TARGET LOCATIONS

Models

src/system/midstream/WSS/models/

Stores

src/system/midstream/WSS/stores/

Engines

src/engines/T1M/WSS/

---

# MODEL

WorkflowDefinition.cs

Fields

WorkflowId
Name
Description
Version
Steps
CreatedAt

Immutable sealed record.

---

# STORE

WorkflowDefinitionStore.cs

ConcurrentDictionary<string, WorkflowDefinition>

Methods

RegisterWorkflow
GetWorkflow
ListWorkflows

---

# ENGINE

WorkflowDefinitionEngine.cs

Methods

RegisterWorkflow
GetWorkflow
ListWorkflows

---

# BUSINESS RULES

Workflow IDs must be unique.

Workflow definitions are immutable.

Versioning handled in later phases.

---

# TESTS

Register workflow
Reject duplicate
Retrieve workflow
List workflows

---

# DEBUG ENDPOINTS

/dev/wss/workflows
/dev/wss/workflows/{id}

---

# NEXT PHASE

2.1.2 Workflow Graph Engine
```

---

# PHASE 2.1.2 — Workflow Graph Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.2 — WORKFLOW GRAPH ENGINE

This phase implements the workflow graph structure.

Workflows are defined as directed graphs.

Each node represents a step.

Edges represent allowed transitions.

---

# MODEL

WorkflowNode.cs

Fields

NodeId
StepType
NextNodes

WorkflowEdge.cs

Fields

FromNode
ToNode

---

# ENGINE

WorkflowGraphEngine.cs

Methods

BuildGraph
ValidateGraph
GetNextSteps

---

# BUSINESS RULES

Graph must have a single start node.

Graph must not contain unreachable nodes.

Cycles allowed only when explicitly defined.

---

# TESTS

Create graph
Validate transitions
Detect unreachable nodes

---

# NEXT PHASE

2.1.3 Workflow Template Engine
```

---

# PHASE 2.1.3 — Workflow Template Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.3 — WORKFLOW TEMPLATE ENGINE

Templates allow reuse of workflow structures.

Example:

Taxi request template
SPV onboarding template
Property letting template

---

# MODEL

WorkflowTemplate.cs

Fields

TemplateId
WorkflowDefinitionId
Parameters
CreatedAt

---

# STORE

WorkflowTemplateStore.cs

---

# ENGINE

WorkflowTemplateEngine.cs

Methods

CreateTemplate
GetTemplate
ListTemplates

---

# TESTS

Create template
Reject duplicates
Retrieve template

---

# NEXT PHASE

2.1.4 Workflow Registry
```

---

# PHASE 2.1.4 — Workflow Registry

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.4 — WORKFLOW REGISTRY

Maintains the registry of all workflow definitions.

---

# STORE

WorkflowRegistryStore.cs

---

# ENGINE

WorkflowRegistryEngine.cs

Methods

RegisterWorkflow
GetWorkflow
ListWorkflows

---

# NEXT PHASE

2.1.5 Workflow Versioning Engine
```

---

# PHASE 2.1.5 — Workflow Versioning Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.5 — WORKFLOW VERSIONING ENGINE

Allows workflows to evolve safely.

Each workflow may have multiple versions.

---

# MODEL

WorkflowVersion.cs

Fields

WorkflowId
Version
CreatedAt
Status

---

# ENGINE

WorkflowVersioningEngine.cs

Methods

CreateVersion
ActivateVersion
GetActiveVersion

---

# NEXT PHASE

2.1.6 Workflow Validation Engine
```

---

# PHASE 2.1.6 — Workflow Validation Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.6 — WORKFLOW VALIDATION ENGINE

Ensures workflows are structurally valid.

---

# ENGINE

WorkflowValidationEngine.cs

Methods

ValidateDefinition
ValidateGraph

---

# VALIDATIONS

No unreachable nodes
Single start node
All steps mapped to engines

---

# NEXT PHASE

2.1.7 Workflow Dependency Engine
```

---

# PHASE 2.1.7 — Workflow Dependency Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.7 — WORKFLOW DEPENDENCY ENGINE

Tracks dependencies between workflows.

Example:

Property onboarding depends on Identity verification.

---

# ENGINE

WorkflowDependencyEngine.cs

Methods

AddDependency
GetDependencies
ValidateDependencies

---

# NEXT PHASE

2.1.8 Workflow Step Engine Mapping
```

---

# PHASE 2.1.8 — Workflow Step Engine Mapping

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.8 — WORKFLOW STEP ENGINE MAPPING

Maps workflow steps to execution engines.

Example

Step: VerifyIdentity → WhyceID engine
Step: RecordPayment → Revenue engine

---

# MODEL

WorkflowStepMapping.cs

Fields

StepId
EngineName
CommandName

---

# ENGINE

WorkflowStepEngineMapping.cs

Methods

MapStep
GetStepEngine

---

# NEXT PHASE

2.1.9 Workflow Instance Registry
```

---

# PHASE 2.1.9 — Workflow Instance Registry

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.9 — WORKFLOW INSTANCE REGISTRY

Stores active workflow instances.

---

# MODEL

WorkflowInstance.cs

Fields

InstanceId
WorkflowId
CurrentStep
Status
StartedAt

---

# STORE

WorkflowInstanceStore.cs

---

# ENGINE

WorkflowInstanceRegistryEngine.cs

Methods

CreateInstance
GetInstance
ListInstances

---

# NEXT PHASE

2.1.10 Workflow State Store
```

---

# PHASE 2.1.10 — Workflow State Store

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.10 — WORKFLOW STATE STORE

Stores runtime workflow state.

---

# MODEL

WorkflowState.cs

Fields

InstanceId
CurrentNode
ContextData
UpdatedAt

---

# STORE

WorkflowStateStore.cs

---

# NEXT PHASE

2.1.11 Workflow Event Router
```

---

# PHASE 2.1.11 — Workflow Event Router

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.11 — WORKFLOW EVENT ROUTER

Routes system events to workflow instances.

Events originate from:

Kafka Event Fabric

---

# ENGINE

WorkflowEventRouter.cs

Methods

RouteEvent
FindWorkflowInstance
AdvanceWorkflow

---

# NEXT PHASE

2.1.12 Workflow Retry Policy Engine
```

---

# PHASE 2.1.12 — Workflow Retry Policy Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.12 — WORKFLOW RETRY POLICY ENGINE

Defines retry policies for workflow failures.

---

# MODEL

RetryPolicy.cs

Fields

MaxRetries
BackoffStrategy
RetryInterval

---

# ENGINE

WorkflowRetryPolicyEngine.cs

Methods

ShouldRetry
CalculateBackoff

---

# NEXT PHASE

2.1.13 Workflow Timeout Engine
```

---

# PHASE 2.1.13 — WORKFLOW TIMEOUT ENGINE

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.13 — WORKFLOW TIMEOUT ENGINE

Detects stalled workflow steps.

---

# ENGINE

WorkflowTimeoutEngine.cs

Methods

CheckTimeout
HandleTimeout

---

# NEXT PHASE

2.1.14 Workflow Instance Lifecycle Engine
```

---

# PHASE 2.1.14 — Workflow Instance Lifecycle Engine

```markdown
# WHYCESPACE WBSM v3
# PHASE 2.1.14 — WORKFLOW INSTANCE LIFECYCLE ENGINE

Controls the lifecycle of workflow instances.

Lifecycle

Created
Running
Paused
Completed
Failed

---

# ENGINE

WorkflowInstanceLifecycleEngine.cs

Methods

StartInstance
PauseInstance
ResumeInstance
CompleteInstance
FailInstance

---

# SUCCESS CRITERIA

Workflow definitions must be deterministic.

Workflow execution must be reproducible.

All workflow state must be auditable.

---

# NEXT MIDSTREAM SYSTEM

HEOS — Human Economic Operating System
```

---

# IMPORTANT ARCHITECTURAL NOTE

Once WSS is implemented, Whycespace will finally have its **execution orchestration layer**.

Full stack will become:

```
Platform APIs
        ↓
WSS (Workflow Structural System)
        ↓
T1M / T2E Engines
        ↓
WhyceChain Evidence
```

This is what allows Whycespace to support **millions of concurrent workflows**.

---

If you want, the **next thing I recommend** (very important before implementation) is:

I can show you the **WSS Runtime Dispatcher Architecture** — the piece that allows **millions of workflows per second without lag**, which we previously discussed with:

```
Kafka
Partitioning
Worker pools
Stateless engines
```

That design is **critical** before implementing phases **2.1.9 → 2.1.14**.
