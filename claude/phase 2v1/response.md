Phase 2.1.1 — Workflow Definition Engine
Files Created
Model — WorkflowDefinition.cs

Immutable sealed record with WorkflowId, Name, Description, Version, Steps (IReadOnlyList<WorkflowStep>), CreatedAt
Store — WorkflowDefinitionStore.cs

ConcurrentDictionary<string, WorkflowDefinition> with Register, Get, GetAll
Rejects duplicate workflow IDs
Engine — WorkflowDefinitionEngine.cs

RegisterWorkflow, GetWorkflow, ListWorkflows
First T1M (Tier 1 Midstream) engine
Engine Project — Whycespace.Engines.T1M.WSS.csproj

Tests — WorkflowDefinitionEngineTests.cs — 6/6 passing

Register workflow, reject duplicate, retrieve workflow, get not found, list workflows, list empty
Files Modified
Whycespace.Engines.csproj — excluded T1M\**
Whycespace.Platform.csproj — added T1M.WSS project reference
Program.cs — registered WorkflowDefinitionStore singleton
DebugController.cs — added /dev/wss/workflows, /dev/wss/workflows/{id}, /dev/wss/workflows/register endpoints

###

Phase 2.1.3 — Workflow Template Engine
Files Created
Model — WorkflowTemplate.cs

Immutable sealed record: TemplateId, WorkflowDefinitionId, Parameters (IReadOnlyDictionary<string, string>), CreatedAt
Store — WorkflowTemplateStore.cs

ConcurrentDictionary<string, WorkflowTemplate> with Register, Get, GetAll
Engine — WorkflowTemplateEngine.cs

CreateTemplate (validates definition exists), GetTemplate, ListTemplates
Tests — WorkflowTemplateEngineTests.cs — 6/6 passing

Create template, reject duplicate, reject invalid definition, retrieve template, not found, list templates
Files Modified
Program.cs — registered WorkflowTemplateStore singleton
DebugController.cs — added /dev/wss/templates, /dev/wss/templates/{id}, /dev/wss/templates/create

###

Phase 2.1.4 — Workflow Registry
Files Created
Model — WorkflowRegistryEntry.cs

Sealed record: WorkflowId, Name, Version, Status (WorkflowRegistryStatus), RegisteredAt
Enum: Active, Inactive
Store — WorkflowRegistryStore.cs

ConcurrentDictionary<string, WorkflowRegistryEntry> with Register, Get, GetAll
Engine — WorkflowRegistryEngine.cs

RegisterWorkflow (validates definition exists, creates Active entry), GetWorkflow, ListWorkflows
Tests — WorkflowRegistryEngineTests.cs — 6/6 passing

Register workflow, reject duplicate, reject missing definition, retrieve, not found, list all
Files Modified
Program.cs — registered WorkflowRegistryStore singleton
DebugController.cs — added /dev/wss/registry, /dev/wss/registry/{id}, /dev/wss/registry/register

###

Phase 2.1.5 — Workflow Versioning Engine
Files Created
Model — WorkflowVersion.cs

Sealed record: WorkflowId, Version, Status (WorkflowVersionStatus), CreatedAt
Enum: Draft, Active, Superseded
Store — WorkflowVersionStore.cs

ConcurrentDictionary<string, List<WorkflowVersion>> — multiple versions per workflow
Store, GetVersions, GetActive, VersionExists, Update
Engine — WorkflowVersioningEngine.cs

CreateVersion — creates as Draft, validates definition exists, rejects duplicate versions
ActivateVersion — supersedes current active, promotes target to Active
GetActiveVersion — returns the single active version
GetVersions — returns all versions ordered
Tests — WorkflowVersioningEngineTests.cs — 8/8 passing

Create draft, reject duplicate version, reject missing definition, activate, supersede previous, get active, no active throws, list ordered
Files Modified
Program.cs — registered WorkflowVersionStore singleton
DebugController.cs — added /dev/wss/versions/{workflowId}, /dev/wss/versions/{workflowId}/active, /dev/wss/versions/create, /dev/wss/versions/activate

###

Phase 2.1.6 is complete. Here's what was implemented:

File: WorkflowValidationEngine.cs

Engine: WorkflowValidationEngine — T0U tier, EngineKind.Validation, implements IEngine

Methods:

ValidateDefinition — checks WorkflowId/Name are non-empty, all steps map to an engine, step IDs are unique, and NextSteps reference valid step IDs
ValidateGraph — performs structural graph validation:
Single start node — exactly one step with no incoming edges
No unreachable nodes — BFS from start node verifies all steps are reachable
ExecuteAsync orchestrates both validations and emits WorkflowValidationPassed or WorkflowValidationFailed events with violation details.

Build succeeds with 0 warnings, 0 errors.

###

Build succeeds. Phase 2.1.7 is complete.

File: WorkflowDependencyEngine.cs

Engine: WorkflowDependencyEngine — T0U tier, EngineKind.Decision, implements IEngine

Methods:

AddDependency(workflowId, dependsOn) — registers that a workflow depends on another (e.g., Property Onboarding depends on Identity Verification). Thread-safe with ConcurrentDictionary + locking.
GetDependencies(workflowId) — returns all dependencies for a given workflow
ValidateDependencies(workflowId, completedWorkflows) — checks which dependencies are unmet by comparing against completed workflows, returns the list of unmet ones
ExecuteAsync dispatches via an action parameter (add, get, validate) and emits events: WorkflowDependencyAdded, WorkflowDependenciesMet, or WorkflowDependenciesNotMet.

###

Phase 2.1.8 is complete.

Model: WorkflowStepMapping.cs — record with StepId, EngineName, CommandName

Engine: WorkflowStepEngineMapping.cs — T0U tier, EngineKind.Decision

Methods:

MapStep(stepId, engineName, commandName) — registers a step-to-engine mapping (e.g., VerifyIdentity → WhyceID engine, ExecuteVerification command). Thread-safe with ConcurrentDictionary.
GetStepEngine(stepId) — resolves the mapping for a given step, returns null if not found
ExecuteAsync dispatches via action parameter (map, get) and emits WorkflowStepMapped events on successful mapping.

###

Phase 2.1.9 is complete.

Model: WorkflowInstanceEntry.cs — record with InstanceId, WorkflowId, CurrentStep, Status, StartedAt (named Entry to avoid collision with the existing runtime WorkflowInstance class)

Store: WorkflowInstanceStore.cs — thread-safe ConcurrentDictionary-backed store with Save, Get, List, and ListByWorkflow

Engine: WorkflowInstanceRegistryEngine.cs — T0U tier, EngineKind.Mutation

Methods:

CreateInstance(workflowId, currentStep) — creates a new instance with Pending status, stores it, emits WorkflowInstanceCreated
GetInstance(instanceId) — retrieves by GUID
ListInstances() — returns all instances, optionally filtered by workflowId

###

Phase 2.1.10 is complete.

Model: WorkflowRuntimeState.cs — record with InstanceId, CurrentNode, ContextData, UpdatedAt (named RuntimeState to avoid collision with the existing WorkflowState contract)

Store: WorkflowStateStore.cs — thread-safe ConcurrentDictionary-backed store with:

Save — persists a state snapshot
Get — retrieves state by instance ID
Update — advances the current node and timestamps, optionally merging new context data

###

Phase 2.1.11 is complete.

Engine: WorkflowEventRouter.cs — T0U tier, EngineKind.Decision

Takes dependencies on WorkflowInstanceStore and WorkflowStateStore from prior phases.

Methods:

RouteEvent(eventType) — finds all workflow instances subscribed to a given event type and returns their IDs
FindWorkflowInstances(eventType) — looks up subscriptions to resolve which instances should receive the event
AdvanceWorkflow(instanceId, nextNode, contextData) — advances a workflow instance to the next node via the state store
ExecuteAsync supports two actions:

subscribe — registers an instance to listen for a specific event type (emits WorkflowEventSubscribed)
route — finds all subscribed instances for an event, advances each workflow, and emits WorkflowEventRouted

