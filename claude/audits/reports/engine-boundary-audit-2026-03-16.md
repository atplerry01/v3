# WHYCESPACE ENGINE BOUNDARY AUDIT REPORT

**Date:** 2026-03-16
**Branch:** `dev_phase2_audit`
**Mode:** WBSM v3 — Read-Only Audit
**Scanner:** Whycespace-Engine-Boundary-Scanner

---

## SECTION 1 — ENGINE TIER OVERVIEW

| Tier | Purpose | Location | Engine Count | File Count |
|------|---------|----------|-------------|------------|
| **T0U** | Constitutional | `src/engines/T0U/` | 4 (WhyceChain, WhyceGovernance, WhyceID, WhycePolicy) | ~128 |
| **T1M** | Orchestration | `src/engines/T1M/` | 1 (WSS) | ~83 |
| **T2E** | Economic Execution | `src/engines/T2E/` | 12+ (HEOS, asset, capital, identity, revenue, vault, cluster, spv, providers) | ~125 |
| **T3I** | Intelligence | `src/engines/T3I/` | 10+ (Capital, Governance, HEOS, WhyceChain, WhyceID, WhycePolicy, analytics, economic, clusters) | ~110 |
| **T4A** | Access / API | `src/engines/T4A/` | 6 (WhycePolicy, API, Auth, Developer, Integration, Operator) | ~9 |
| | | **TOTAL** | **33+** | **~455** |

---

## SECTION 2 — VIOLATIONS DETECTED

### V-001: Stores Inside T1M/WSS Engine

| Field | Value |
|-------|-------|
| **Type** | Store inside engine |
| **Severity** | **Critical** |
| **Location** | `src/engines/T1M/WSS/stores/` (14 files) |

**Files:**

| # | File |
|---|------|
| 1 | `src/engines/T1M/WSS/stores/WorkflowInstanceStore.cs` |
| 2 | `src/engines/T1M/WSS/stores/WorkflowDefinitionStore.cs` |
| 3 | `src/engines/T1M/WSS/stores/WorkflowRegistryStore.cs` |
| 4 | `src/engines/T1M/WSS/stores/WorkflowStateStore.cs` |
| 5 | `src/engines/T1M/WSS/stores/WorkflowTimeoutStore.cs` |
| 6 | `src/engines/T1M/WSS/stores/WorkflowRetryStore.cs` |
| 7 | `src/engines/T1M/WSS/stores/WorkflowVersionStore.cs` |
| 8 | `src/engines/T1M/WSS/stores/WorkflowTemplateStore.cs` |
| 9 | `src/engines/T1M/WSS/stores/WorkflowEngineMappingStore.cs` |
| 10 | `src/engines/T1M/WSS/stores/WorkflowInstanceRegistryStore.cs` |
| 11 | `src/engines/T1M/WSS/stores/WssWorkflowStateStore.cs` |
| 12 | `src/engines/T1M/WSS/stores/IWorkflowRetryStore.cs` |
| 13 | `src/engines/T1M/WSS/stores/IWorkflowTimeoutStore.cs` |
| 14 | `src/engines/T1M/WSS/stores/IWssWorkflowStateStore.cs` |

**Explanation:** These are `ConcurrentDictionary`-backed persistence classes with Save/Get/Update/Delete methods. Engines must be stateless. Stores belong in `src/runtime/persistence/`.

---

### V-002: Registries Inside T1M/WSS Engine

| Field | Value |
|-------|-------|
| **Type** | Registry inside engine |
| **Severity** | **Critical** |
| **Location** | `src/engines/T1M/WSS/registry/` and `src/engines/T1M/WSS/instance/` |

**Files:**

| # | File |
|---|------|
| 1 | `src/engines/T1M/WSS/registry/WorkflowRegistry.cs` |
| 2 | `src/engines/T1M/WSS/registry/IWorkflowRegistry.cs` |
| 3 | `src/engines/T1M/WSS/instance/WorkflowInstanceRegistry.cs` |
| 4 | `src/engines/T1M/WSS/instance/IWorkflowInstanceRegistry.cs` |

**Explanation:** Stateful in-memory registries with CRUD operations. Registries must live in `src/systems/`. Engines must not maintain state dictionaries.

---

### V-003: Runtime Logic Inside T1M/WSS Engine

| Field | Value |
|-------|-------|
| **Type** | Runtime logic inside engine |
| **Severity** | **Critical** |
| **Location** | `src/engines/T1M/WSS/runtime/` (13 files) |

**Files:**

| # | File | Violation |
|---|------|-----------|
| 1 | `src/engines/T1M/WSS/runtime/RuntimeDispatcherEngine.cs` | Dispatcher pattern — routes execution to target engines |
| 2 | `src/engines/T1M/WSS/runtime/WorkflowEventRouter.cs` | Message routing with subscriber registry + Kafka publishing |
| 3 | `src/engines/T1M/WSS/runtime/PartitionRouterEngine.cs` | Partition resolution — belongs in partition runtime |
| 4 | `src/engines/T1M/WSS/runtime/WorkflowTimeoutEngine.cs` | Timeout management with store dependency |
| 5 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyEngine.cs` | Retry policy with stateful counter tracking |
| 6 | `src/engines/T1M/WSS/runtime/WorkflowSchedulerEngine.cs` | Scheduling — runtime infrastructure |
| 7 | `src/engines/T1M/WSS/runtime/WorkflowLifecycleEngine.cs` | Lifecycle management |
| 8 | `src/engines/T1M/WSS/runtime/IWorkflowEventRouter.cs` | Interface for V-003.2 |
| 9 | `src/engines/T1M/WSS/runtime/IWorkflowLifecycleEngine.cs` | Interface for V-003.7 |
| 10 | `src/engines/T1M/WSS/runtime/IWorkflowTimeoutEngine.cs` | Interface for V-003.4 |
| 11 | `src/engines/T1M/WSS/runtime/IWorkflowRetryPolicyEngine.cs` | Interface for V-003.5 |
| 12 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyCommand.cs` | Command model for retry |
| 13 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyResult.cs` | Result model for retry |

**Explanation:** Dispatchers, routers, retry policies, timeouts, and schedulers are runtime infrastructure. They must live in `src/runtime/`.

---

### V-004: Infrastructure Dependency — Kafka in Engine

| Field | Value |
|-------|-------|
| **Type** | Infrastructure dependency inside engine |
| **Severity** | **Critical** |
| **Location** | `src/engines/T1M/WSS/runtime/WorkflowEventRouter.cs` |

**Explanation:** Direct dependency on `KafkaEventPublisher` with calls to `PublishToTopicAsync()`. Engines must never reference Kafka clients directly. Event publishing must go through the event fabric in `src/runtime/event-fabric/`.

---

### V-005: Engine-to-Engine Project Reference — T3I.WhycePolicy to T0U.WhycePolicy

| Field | Value |
|-------|-------|
| **Type** | Engine-to-engine dependency |
| **Severity** | **Critical** |
| **Location** | `src/engines/T3I/WhycePolicy/Whycespace.Engines.T3I.WhycePolicy.csproj` |

**Affected files:**

| # | File |
|---|------|
| 1 | `src/engines/T3I/WhycePolicy/PolicyMonitoringEngine.cs` |
| 2 | `src/engines/T3I/WhycePolicy/PolicyMonitoringInput.cs` |
| 3 | `src/engines/T3I/WhycePolicy/PolicyConflictAnalysisEngine.cs` |
| 4 | `src/engines/T3I/WhycePolicy/PolicyConflictAnalysisInput.cs` |

**Explanation:** T3I intelligence engines reference T0U constitutional engines via `using Whycespace.Engines.T0U.WhycePolicy`. Engines must communicate via events only, never direct references.

---

### V-006: Engine-to-Engine Project Reference — T0U.WhyceGovernance to T0U.WhyceChain

| Field | Value |
|-------|-------|
| **Type** | Engine-to-engine dependency |
| **Severity** | **Critical** |
| **Location** | `src/engines/T0U/WhyceGovernance/Whycespace.Engines.T0U.WhyceGovernance.csproj` |

**Affected files:**

| # | File |
|---|------|
| 1 | `src/engines/T0U/WhyceGovernance/GovernanceAuditEngine.cs` |
| 2 | `src/engines/T0U/WhyceGovernance/GovernanceEvidenceRecorder.cs` |

**Explanation:** WhyceGovernance directly references WhyceChain for evidence anchoring. This must be decoupled via events or shared contracts in `src/shared/`.

---

### V-007: Engine-to-Engine References — T2E Vault Adapters to T0U Engines

| Field | Value |
|-------|-------|
| **Type** | Engine-to-engine dependency |
| **Severity** | **High** |
| **Location** | `src/engines/T2E/economic/vault/adapters/` |

**Affected files:**

| # | File | References |
|---|------|-----------|
| 1 | `src/engines/T2E/economic/vault/adapters/VaultIdentityAuthorizationAdapter.cs` | `Whycespace.Engines.T0U.WhyceID` |
| 2 | `src/engines/T2E/economic/vault/adapters/VaultEvidenceAnchorAdapter.cs` | `Whycespace.Engines.T0U.WhyceChain` |

**Explanation:** T2E vault adapters directly reference T0U engines. The adapter pattern is correct conceptually, but the adapters should reference shared contracts, not engine implementations.

---

### V-008: System Logic Inside T2E Engines

| Field | Value |
|-------|-------|
| **Type** | System logic inside engine |
| **Severity** | **Medium** |
| **Location** | `src/engines/T2E/system/` |

**Files:**

| # | File | Concern |
|---|------|---------|
| 1 | `src/engines/T2E/system/cluster/ClusterCreationEngine.cs` | Cluster creation — system-level operation |
| 2 | `src/engines/T2E/system/spv/SpvCreationEngine.cs` | SPV creation — system-level operation |
| 3 | `src/engines/T2E/system/providers/ClusterProviderRegistrationEngine.cs` | Provider registration — system-level operation |

**Explanation:** Cluster/SPV creation and provider registration are system-level operations. These engines emit creation events, which is borderline acceptable, but the `system/` subfolder naming signals a layer confusion.

---

### V-009: API Routing Logic Inside T4A Engine

| Field | Value |
|-------|-------|
| **Type** | Infrastructure logic inside engine |
| **Severity** | **Medium** |
| **Location** | `src/engines/T4A/api/APIEngine.cs` |

**Explanation:** API command routing and route validation are infrastructure concerns belonging in `src/platform/`. T4A engines should enforce access policies, not route HTTP requests.

---

### Violation Summary

| Severity | Count |
|----------|-------|
| Critical | 6 |
| High | 1 |
| Medium | 2 |
| **Total** | **9 distinct violations** |

---

## SECTION 3 — FILES THAT MUST MOVE

| # | Current Location | Target Location | Reason |
|---|-----------------|-----------------|--------|
| 1 | `src/engines/T1M/WSS/stores/WorkflowInstanceStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 2 | `src/engines/T1M/WSS/stores/WorkflowDefinitionStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 3 | `src/engines/T1M/WSS/stores/WorkflowRegistryStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 4 | `src/engines/T1M/WSS/stores/WorkflowStateStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 5 | `src/engines/T1M/WSS/stores/WorkflowTimeoutStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 6 | `src/engines/T1M/WSS/stores/WorkflowRetryStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 7 | `src/engines/T1M/WSS/stores/WorkflowVersionStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 8 | `src/engines/T1M/WSS/stores/WorkflowTemplateStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 9 | `src/engines/T1M/WSS/stores/WorkflowEngineMappingStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 10 | `src/engines/T1M/WSS/stores/WorkflowInstanceRegistryStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 11 | `src/engines/T1M/WSS/stores/WssWorkflowStateStore.cs` | `src/runtime/persistence/workflow/` | Store must live in runtime persistence |
| 12 | `src/engines/T1M/WSS/stores/IWorkflowRetryStore.cs` | `src/runtime/persistence/workflow/` | Interface follows implementation |
| 13 | `src/engines/T1M/WSS/stores/IWorkflowTimeoutStore.cs` | `src/runtime/persistence/workflow/` | Interface follows implementation |
| 14 | `src/engines/T1M/WSS/stores/IWssWorkflowStateStore.cs` | `src/runtime/persistence/workflow/` | Interface follows implementation |
| 15 | `src/engines/T1M/WSS/registry/WorkflowRegistry.cs` | `src/systems/midstream/WSS/registry/` | Registry must live in system layer |
| 16 | `src/engines/T1M/WSS/registry/IWorkflowRegistry.cs` | `src/systems/midstream/WSS/registry/` | Registry must live in system layer |
| 17 | `src/engines/T1M/WSS/instance/WorkflowInstanceRegistry.cs` | `src/systems/midstream/WSS/registry/` | Registry must live in system layer |
| 18 | `src/engines/T1M/WSS/instance/IWorkflowInstanceRegistry.cs` | `src/systems/midstream/WSS/registry/` | Registry must live in system layer |
| 19 | `src/engines/T1M/WSS/runtime/RuntimeDispatcherEngine.cs` | `src/runtime/dispatcher/workflow/` | Dispatcher must live in runtime |
| 20 | `src/engines/T1M/WSS/runtime/WorkflowEventRouter.cs` | `src/runtime/event-fabric-runtime/workflow/` | Event routing must live in runtime |
| 21 | `src/engines/T1M/WSS/runtime/IWorkflowEventRouter.cs` | `src/runtime/event-fabric-runtime/workflow/` | Interface follows implementation |
| 22 | `src/engines/T1M/WSS/runtime/PartitionRouterEngine.cs` | `src/runtime/partition/` | Partition logic must live in runtime |
| 23 | `src/engines/T1M/WSS/runtime/WorkflowTimeoutEngine.cs` | `src/runtime/reliability-runtime/timeout/` | Timeout must live in runtime reliability |
| 24 | `src/engines/T1M/WSS/runtime/IWorkflowTimeoutEngine.cs` | `src/runtime/reliability-runtime/timeout/` | Interface follows implementation |
| 25 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyEngine.cs` | `src/runtime/reliability-runtime/retry/` | Retry must live in runtime reliability |
| 26 | `src/engines/T1M/WSS/runtime/IWorkflowRetryPolicyEngine.cs` | `src/runtime/reliability-runtime/retry/` | Interface follows implementation |
| 27 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyCommand.cs` | `src/runtime/reliability-runtime/retry/` | Command model follows engine |
| 28 | `src/engines/T1M/WSS/runtime/WorkflowRetryPolicyResult.cs` | `src/runtime/reliability-runtime/retry/` | Result model follows engine |
| 29 | `src/engines/T1M/WSS/runtime/WorkflowSchedulerEngine.cs` | `src/runtime/workflow-runtime/` | Scheduler must live in runtime |
| 30 | `src/engines/T1M/WSS/runtime/WorkflowLifecycleEngine.cs` | `src/runtime/workflow-runtime/` | Lifecycle must live in runtime |
| 31 | `src/engines/T1M/WSS/runtime/IWorkflowLifecycleEngine.cs` | `src/runtime/workflow-runtime/` | Interface follows implementation |

**Total files to move: 31**

---

## SECTION 4 — NAMESPACE VIOLATIONS

| # | Violating Namespace | Referenced From | Violation |
|---|---------------------|----------------|-----------|
| 1 | `Whycespace.Engines.T0U.WhyceChain` | `src/engines/T0U/WhyceGovernance/GovernanceAuditEngine.cs` | Engine-to-engine cross-reference within T0U |
| 2 | `Whycespace.Engines.T0U.WhyceChain` | `src/engines/T0U/WhyceGovernance/GovernanceEvidenceRecorder.cs` | Engine-to-engine cross-reference within T0U |
| 3 | `Whycespace.Engines.T0U.WhycePolicy` | `src/engines/T3I/WhycePolicy/PolicyMonitoringEngine.cs` | Cross-tier engine dependency (T3I references T0U) |
| 4 | `Whycespace.Engines.T0U.WhycePolicy` | `src/engines/T3I/WhycePolicy/PolicyMonitoringInput.cs` | Cross-tier engine dependency (T3I references T0U) |
| 5 | `Whycespace.Engines.T0U.WhycePolicy` | `src/engines/T3I/WhycePolicy/PolicyConflictAnalysisEngine.cs` | Cross-tier engine dependency (T3I references T0U) |
| 6 | `Whycespace.Engines.T0U.WhycePolicy` | `src/engines/T3I/WhycePolicy/PolicyConflictAnalysisInput.cs` | Cross-tier engine dependency (T3I references T0U) |
| 7 | `Whycespace.Engines.T0U.WhyceID` | `src/engines/T2E/economic/vault/adapters/VaultIdentityAuthorizationAdapter.cs` | Cross-tier engine dependency (T2E references T0U) |
| 8 | `Whycespace.Engines.T0U.WhyceChain` | `src/engines/T2E/economic/vault/adapters/VaultEvidenceAnchorAdapter.cs` | Cross-tier engine dependency (T2E references T0U) |
| 9 | `KafkaEventPublisher` (infrastructure) | `src/engines/T1M/WSS/runtime/WorkflowEventRouter.cs` | Infrastructure dependency inside engine |

---

## SECTION 5 — ENGINE PURITY CHECK

### T0U — Constitutional Engines

| Engine | Stateless | No Persistence | No Registry | No Infrastructure |
|--------|-----------|---------------|-------------|-------------------|
| WhyceChain | ✓ | ✓ | ✓ | ✓ |
| WhyceGovernance | ✓ | ✓ | ✓ | ✗ (references WhyceChain engine) |
| WhyceID | ✓ | ✓ | ✓ | ✓ |
| WhycePolicy | ✓ | ✓ | ✓ | ✓ |

### T1M — Orchestration Engines

| Engine | Stateless | No Persistence | No Registry | No Infrastructure |
|--------|-----------|---------------|-------------|-------------------|
| WSS | ✗ | ✗ (14 stores) | ✗ (4 registries) | ✗ (Kafka, dispatchers, routers) |

### T2E — Execution Engines

| Engine | Stateless | No Persistence | No Registry | No Infrastructure |
|--------|-----------|---------------|-------------|-------------------|
| HEOS | ✓ | ✓ | ✓ | ✓ |
| core/asset | ✓ | ✓ | ✓ | ✓ |
| core/capital | ✓ | ✓ | ✓ | ✓ |
| core/identity | ✓ | ✓ | ✓ | ✓ |
| core/revenue | ✓ | ✓ | ✓ | ✓ |
| core/vault | ✓ | ✓ | ✓ | ✓ |
| economic/vault | ✓ | ✓ | ✓ | ✗ (adapters reference T0U engines) |
| system/cluster | ✓ | ✓ | ✓ | ✓ (borderline — system ops) |
| system/spv | ✓ | ✓ | ✓ | ✓ (borderline — system ops) |
| system/providers | ✓ | ✓ | ✓ | ✓ (borderline — system ops) |
| clusters/mobility | ✓ | ✓ | ✓ | ✓ |
| clusters/property | ✓ | ✓ | ✓ | ✓ |

### T3I — Intelligence Engines

| Engine | Stateless | No Persistence | No Registry | No Infrastructure |
|--------|-----------|---------------|-------------|-------------------|
| Capital | ✓ | ✓ | ✓ | ✓ |
| Governance | ✓ | ✓ | ✓ | ✓ |
| HEOS | ✓ | ✓ | ✓ | ✓ |
| WhyceChain | ✓ | ✓ | ✓ | ✓ |
| WhyceID | ✓ | ✓ | ✓ | ✓ |
| WhycePolicy | ✓ | ✓ | ✓ | ✗ (references T0U.WhycePolicy) |
| core/* | ✓ | ✓ | ✓ | ✓ |
| economic/* | ✓ | ✓ | ✓ | ✓ |
| clusters/* | ✓ | ✓ | ✓ | ✓ |

### T4A — Access Engines

| Engine | Stateless | No Persistence | No Registry | No Infrastructure |
|--------|-----------|---------------|-------------|-------------------|
| WhycePolicy | ✓ | ✓ | ✓ | ✓ |
| API | ✓ | ✓ | ✓ | ✗ (route validation = infra) |
| Auth | ✓ | ✓ | ✓ | ✓ |
| Developer | ✓ | ✓ | ✓ | ✓ |
| Integration | ✓ | ✓ | ✓ | ✓ |
| Operator | ✓ | ✓ | ✓ | ✓ |

---

## SECTION 6 — SAFE MIGRATION PLAN

### Phase 1: Extract T1M/WSS Stores (Critical — 14 files)

**Create folder:** `src/runtime/persistence/workflow/`

**Move files:**
All 14 files from `src/engines/T1M/WSS/stores/` to `src/runtime/persistence/workflow/`

**Namespace change:**
`Whycespace.Engines.T1M.WSS.Stores` to `Whycespace.Runtime.Persistence.Workflow`

**Dependency update:**
- T1M WSS engines that inject stores must reference `Whycespace.Runtime.Persistence.Workflow` via interface injection
- Update `Whycespace.Engines.T1M.WSS.csproj` to reference the runtime persistence project
- Deduplicate against existing stores in `src/systems/midstream/WSS/stores/`

---

### Phase 2: Extract T1M/WSS Registries (Critical — 4 files)

**Target folder:** `src/systems/midstream/WSS/registry/` (already exists — merge required)

**Move files:**
- `src/engines/T1M/WSS/registry/WorkflowRegistry.cs` to `src/systems/midstream/WSS/registry/`
- `src/engines/T1M/WSS/registry/IWorkflowRegistry.cs` to `src/systems/midstream/WSS/registry/`
- `src/engines/T1M/WSS/instance/WorkflowInstanceRegistry.cs` to `src/systems/midstream/WSS/registry/`
- `src/engines/T1M/WSS/instance/IWorkflowInstanceRegistry.cs` to `src/systems/midstream/WSS/registry/`

**Namespace change:**
- `Whycespace.Engines.T1M.WSS.Registry` to `Whycespace.System.WSS.Registry`
- `Whycespace.Engines.T1M.WSS.Instance` to `Whycespace.System.WSS.Registry`

**Note:** Deduplicate against existing system-layer registry definitions before merging.

---

### Phase 3: Extract T1M/WSS Runtime Components (Critical — 13 files)

**Move targets:**

| File | Target |
|------|--------|
| `RuntimeDispatcherEngine.cs` | `src/runtime/dispatcher/workflow/` |
| `WorkflowEventRouter.cs` + `IWorkflowEventRouter.cs` | `src/runtime/event-fabric-runtime/workflow/` |
| `PartitionRouterEngine.cs` | `src/runtime/partition/` |
| `WorkflowTimeoutEngine.cs` + `IWorkflowTimeoutEngine.cs` | `src/runtime/reliability-runtime/timeout/` |
| `WorkflowRetryPolicyEngine.cs` + `IWorkflowRetryPolicyEngine.cs` + command + result | `src/runtime/reliability-runtime/retry/` |
| `WorkflowSchedulerEngine.cs` | `src/runtime/workflow-runtime/` |
| `WorkflowLifecycleEngine.cs` + `IWorkflowLifecycleEngine.cs` | `src/runtime/workflow-runtime/` |

**Namespace changes:**
- `Whycespace.Engines.T1M.WSS.Runtime` to appropriate `Whycespace.Runtime.*` namespaces per target

**Critical:** Remove `KafkaEventPublisher` dependency from `WorkflowEventRouter` — replace with `IEventBus` from `src/shared/contracts/`.

---

### Phase 4: Decouple Engine-to-Engine References (Critical — 3 csproj changes)

**Step 4a: T3I.WhycePolicy to T0U.WhycePolicy**
- Remove `ProjectReference` to `T0U.WhycePolicy` from `Whycespace.Engines.T3I.WhycePolicy.csproj`
- Extract shared types (`PolicyEvaluationResult`, `PolicyRecord`, etc.) to `src/shared/contracts/policy/`
- T3I engines reference shared contracts, not T0U engine types

**Step 4b: T0U.WhyceGovernance to T0U.WhyceChain**
- Remove `ProjectReference` to `T0U.WhyceChain` from `Whycespace.Engines.T0U.WhyceGovernance.csproj`
- Extract shared evidence/anchor contracts to `src/shared/contracts/chain/`
- Governance emits evidence-anchor-requested events; WhyceChain handles them asynchronously

**Step 4c: T2E Vault Adapters to T0U Engines**
- Refactor `VaultIdentityAuthorizationAdapter.cs` and `VaultEvidenceAnchorAdapter.cs`
- Replace direct engine references with shared interface contracts from `src/shared/contracts/`

---

### Phase 5: Review Borderline Items (Medium priority)

- Evaluate T2E `system/` engines for potential reclassification to system layer
- Review T4A `APIEngine` for platform migration

---

## SECTION 7 — FINAL ARCHITECTURE SCORE

| Category | Score | Max | Notes |
|----------|-------|-----|-------|
| **Engine Purity** | 12 | 25 | T1M/WSS has massive violations (stores, registries, runtime, infra). Other tiers mostly clean. |
| **Layer Separation** | 14 | 20 | Engine-to-engine cross-references break tier isolation. System layer has scope creep. |
| **Runtime Isolation** | 13 | 20 | Runtime components correctly placed, but duplicated inside T1M/WSS engines. |
| **System Separation** | 15 | 20 | System layer exists and is populated. Some stores and orchestration logic leak boundaries. |
| **Dependency Hygiene** | 11 | 15 | 3 cross-engine project references. 1 direct Kafka dependency in engine. No EF/Dapper/Redis violations. |
| **TOTAL** | **65** | **100** | |

### Score Interpretation

| Range | Rating |
|-------|--------|
| 90-100 | Compliant |
| 75-89 | Minor violations |
| 60-74 | Significant violations — refactoring required |
| 40-59 | Major architectural drift |
| 0-39 | Non-compliant |

**Rating: Significant violations — refactoring required**

The primary source of debt is **T1M/WSS** which contains 31 files that violate engine boundary rules (14 stores, 4 registries, 13 runtime components). The remaining tiers are largely clean, with isolated cross-engine reference violations in T0U.WhyceGovernance, T2E.vault.adapters, and T3I.WhycePolicy.

**Priority:** Phases 1-3 (T1M/WSS extraction) should be executed first as they account for approximately 80% of detected violations.

---

**End of Audit Report**
