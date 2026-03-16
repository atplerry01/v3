You are performing a **controlled architecture refactor** on the Whycespace repository.

This repository operates under:

WHYCESPACE BUILD STRICT MODE v3 (WBSM v3)

Your task is to fix **engine boundary violations** discovered in the audit report.

This refactor must **NOT change business logic**.

Only perform:

• file relocation  
• namespace updates  
• project reference updates  
• dependency corrections

Do NOT modify algorithm behavior.

---

# ARCHITECTURE RULES

Whycespace uses the following layered architecture:

src/
  domain/
  engines/
  runtime/
  system/
  platform/
  shared/

Engine tiers:

T0U → Constitutional Engines  
T1M → Orchestration Engines  
T2E → Economic Execution Engines  
T3I → Intelligence Engines  
T4A → Access Engines  

Engines must be:

• stateless  
• deterministic  
• compute-only  

Engines must NOT contain:

• stores
• registries
• runtime dispatchers
• schedulers
• Kafka clients
• infrastructure dependencies

---

# REFACTOR OBJECTIVE

Clean the **T1M/WSS engine folder** by extracting:

1️⃣ Stores  
2️⃣ Registries  
3️⃣ Runtime infrastructure  

into their proper layers.

Current violating folder:

src/engines/T1M/WSS/

---

# PHASE 1 — MOVE STORES TO RUNTIME PERSISTENCE

Create folder:

src/runtime/persistence/workflow/

Move the following files:

src/engines/T1M/WSS/stores/WorkflowInstanceStore.cs  
src/engines/T1M/WSS/stores/WorkflowDefinitionStore.cs  
src/engines/T1M/WSS/stores/WorkflowRegistryStore.cs  
src/engines/T1M/WSS/stores/WorkflowStateStore.cs  
src/engines/T1M/WSS/stores/WorkflowTimeoutStore.cs  
src/engines/T1M/WSS/stores/WorkflowRetryStore.cs  
src/engines/T1M/WSS/stores/WorkflowVersionStore.cs  
src/engines/T1M/WSS/stores/WorkflowTemplateStore.cs  
src/engines/T1M/WSS/stores/WorkflowEngineMappingStore.cs  
src/engines/T1M/WSS/stores/WorkflowInstanceRegistryStore.cs  
src/engines/T1M/WSS/stores/WssWorkflowStateStore.cs  
src/engines/T1M/WSS/stores/IWorkflowRetryStore.cs  
src/engines/T1M/WSS/stores/IWorkflowTimeoutStore.cs  
src/engines/T1M/WSS/stores/IWssWorkflowStateStore.cs  

Update namespace:

FROM

Whycespace.Engines.T1M.WSS.Stores

TO

Whycespace.Runtime.Persistence.Workflow

Update imports in any referencing files.

---

# PHASE 2 — MOVE REGISTRIES TO SYSTEM LAYER

Move:

src/engines/T1M/WSS/registry/WorkflowRegistry.cs  
src/engines/T1M/WSS/registry/IWorkflowRegistry.cs  
src/engines/T1M/WSS/instance/WorkflowInstanceRegistry.cs  
src/engines/T1M/WSS/instance/IWorkflowInstanceRegistry.cs  

TO

src/systems/midstream/WSS/registry/

Update namespace:

FROM

Whycespace.Engines.T1M.WSS.Registry

TO

Whycespace.System.WSS.Registry

Update all references.

---

# PHASE 3 — MOVE RUNTIME COMPONENTS

Move dispatcher:

src/engines/T1M/WSS/runtime/RuntimeDispatcherEngine.cs

TO

src/runtime/dispatcher/workflow/

---

Move event routing:

src/engines/T1M/WSS/runtime/WorkflowEventRouter.cs  
src/engines/T1M/WSS/runtime/IWorkflowEventRouter.cs  

TO

src/runtime/event-fabric-runtime/workflow/

Replace dependency:

KafkaEventPublisher

with

IEventBus from:

src/shared/contracts/events/

---

Move partition routing:

src/engines/T1M/WSS/runtime/PartitionRouterEngine.cs

TO

src/runtime/partition/

---

Move timeout handling:

src/engines/T1M/WSS/runtime/WorkflowTimeoutEngine.cs  
src/engines/T1M/WSS/runtime/IWorkflowTimeoutEngine.cs  

TO

src/runtime/reliability-runtime/timeout/

---

Move retry handling:

src/engines/T1M/WSS/runtime/WorkflowRetryPolicyEngine.cs  
src/engines/T1M/WSS/runtime/IWorkflowRetryPolicyEngine.cs  
src/engines/T1M/WSS/runtime/WorkflowRetryPolicyCommand.cs  
src/engines/T1M/WSS/runtime/WorkflowRetryPolicyResult.cs  

TO

src/runtime/reliability-runtime/retry/

---

Move workflow lifecycle management:

src/engines/T1M/WSS/runtime/WorkflowSchedulerEngine.cs  
src/engines/T1M/WSS/runtime/WorkflowLifecycleEngine.cs  
src/engines/T1M/WSS/runtime/IWorkflowLifecycleEngine.cs  

TO

src/runtime/workflow-runtime/

---

# PHASE 4 — UPDATE PROJECT REFERENCES

Update:

Whycespace.Engines.T1M.WSS.csproj

Remove references to moved store/runtime files.

Add project references to:

Whycespace.Runtime.Persistence  
Whycespace.Runtime.WorkflowRuntime  
Whycespace.Runtime.Dispatcher

---

# PHASE 5 — UPDATE NAMESPACES

After moving files update namespaces to match new folder structure.

Examples:

Whycespace.Runtime.Persistence.Workflow  
Whycespace.Runtime.WorkflowRuntime  
Whycespace.Runtime.Dispatcher.Workflow  
Whycespace.Runtime.Reliability.Timeout  
Whycespace.Runtime.Reliability.Retry  

---

# VALIDATION CHECKLIST

After refactor confirm:

✔ Solution builds successfully  
✔ No missing namespaces  
✔ No circular dependencies  
✔ No engine contains stores or registries  
✔ WSS engines remain stateless  
✔ Runtime logic only exists in runtime/  
✔ No Kafka dependency inside engines  

---

# OUTPUT REQUIRED

Provide a summary:

1️⃣ Files moved  
2️⃣ Namespaces updated  
3️⃣ Project references updated  
4️⃣ Remaining architecture warnings  
5️⃣ Final build status