# WHYCESPACE — DOWNSTREAM SYSTEM FULL REFACTOR
# WBSM v3 STRICT MODE — CANONICAL STRUCTURE ENFORCEMENT

You are operating under **WBSM v3 STRICT MODE**.

NON-NEGOTIABLE RULES:

1. NO DRIFT — Must match canonical structure EXACTLY
2. STRICT LAYERING — work / cwg / spv separation is mandatory
3. NO DUPLICATION — Single responsibility per module
4. EVENT-DRIVEN — No direct cross-layer calls
5. POLICY-READY — Must support WHYCEPOLICY integration
6. BUILD MUST SUCCEED — No broken references

---

# OBJECTIVE

Refactor:

src/systems/downstream/

Into a **fully canonical downstream system** with:

- Layered execution (work / cwg / spv)
- Canonical cluster system
- Coordination layer
- Vault system under CWG
- Full SPV lifecycle system

---

# 🔴 STEP 1 — CREATE CANONICAL STRUCTURE

Create:

src/systems/downstream/

├── work/
├── cwg/
├── spv/
├── clusters/
└── coordination/

---

# 🔴 STEP 2 — MOVE WORK EXECUTION (Layer 1)

CREATE:

work/

STRUCTURE:

work/
 ├── execution/
 ├── tasks/
 ├── mobility/
 ├── property/
 └── shared/

MOVE:

- mobility/projections → work/mobility/projections/
- property/projections → work/property/projections/

CREATE NEW FILES:

- WorkExecutionCoordinator.cs
- WorkCommandRouter.cs
- WorkExecutionContext.cs
- TaskDefinition.cs
- TaskRegistry.cs
- TaskAssignment.cs
- WorkExecutionPolicy.cs

---

# 🔴 STEP 3 — MOVE ECONOMIC + VAULT → CWG (Layer 2)

CREATE:

cwg/

STRUCTURE:

cwg/
 ├── participants/
 ├── contributions/
 ├── vaults/
 │   ├── allocation/
 │   ├── ledger/
 │   ├── registry/
 │   ├── participants/
 │   ├── policy/
 │   └── transactions/
 └── governance/

MOVE:

economic/vault/* → cwg/vaults/

RENAME:

transaction-registry → transactions/

CREATE NEW:

- CWGParticipantRegistry.cs
- CWGParticipantRecord.cs
- TrustScoreModel.cs
- ContributionRegistry.cs
- ContributionRecord.cs
- ContributionPolicy.cs
- CWGPolicyAdapter.cs
- ParticipationRules.cs

DELETE:

economic/
EconomicCoordinator.cs

---

# 🔴 STEP 4 — BUILD SPV SYSTEM (Layer 3)

CREATE:

spv/

STRUCTURE:

spv/
 ├── lifecycle/
 ├── registry/
 ├── capital/
 ├── governance/
 └── orchestration/

REPLACE:

SpvManager.cs → FULL SYSTEM

CREATE FILES:

lifecycle/
- SpvLifecycleManager.cs
- SpvCreationPolicy.cs
- SpvTerminationPolicy.cs
- SpvStateMachine.cs

registry/
- SpvRegistry.cs
- SpvRegistryRecord.cs

capital/
- SpvCapitalStructure.cs
- InvestorAllocationModel.cs

governance/
- SpvGovernancePolicy.cs
- VotingModel.cs

orchestration/
- SpvOrchestrator.cs

---

# 🔴 STEP 5 — REBUILD CLUSTERS (CRITICAL)

CREATE:

clusters/

STRUCTURE:

clusters/
 ├── administration/
 ├── registry/
 ├── definition/
 ├── classification/
 ├── lifecycle/
 ├── providers/
 └── implementations/

---

## MOVE EXISTING:

- ClusterDefinition.cs → definition/
- ClusterRegistry.cs → registry/

---

## CREATE ADMINISTRATION:

- ClusterAdministrationManager.cs
- ClusterAdministratorRecord.cs
- ClusterAdministrationPolicy.cs
- ClusterAdministrationContext.cs

---

## CREATE PROVIDERS:

- ClusterProviderRegistry.cs
- ClusterProviderRecord.cs
- ClusterProviderType.cs
- ClusterProviderPolicy.cs

---

## CREATE LIFECYCLE:

- ClusterLifecycleManager.cs
- ClusterLifecycleState.cs
- ClusterLifecyclePolicy.cs

---

## CREATE CLASSIFICATION:

- ClusterClassification.cs

---

## MOVE IMPLEMENTATIONS:

WhyceMobility.cs → implementations/WhyceMobility/
WhyceProperty.cs → implementations/WhyceProperty/

REFactor into:

WhyceMobility/
- WhyceMobilityCluster.cs
- WhyceMobilityAuthorities.cs
- WhyceMobilitySubClusters.cs

WhyceProperty/
- WhycePropertyCluster.cs
- WhycePropertyAuthorities.cs
- WhycePropertySubClusters.cs

---

# 🔴 STEP 6 — CREATE COORDINATION LAYER

CREATE:

coordination/

FILES:

- DownstreamCoordinator.cs
- ExecutionToSpvBridge.cs
- WorkToCapitalFlowMapper.cs
- ClusterExecutionRouter.cs

PURPOSE:

- Connect work → cwg → spv → clusters
- Maintain system flow consistency

---

# 🔴 STEP 7 — REMOVE INVALID STRUCTURE

DELETE:

- economic/
- SpvManager.cs
- flat clusters files

---

# 🔴 STEP 8 — ENFORCE RULES

ENSURE:

- No direct calls:
  - work → spv ❌
  - cwg → work ❌
- All interactions via:
  - coordination
  - events

---

# 🔴 STEP 9 — POLICY INTEGRATION READY

Ensure these exist:

- cwg/governance/
- spv/governance/
- work/shared/

These must be ready for WHYCEPOLICY integration

---

# 🔴 STEP 10 — BUILD VALIDATION

ENSURE:

- Solution builds successfully
- Namespaces are correct
- No missing references
- No circular dependencies

---

# 🔴 STEP 11 — OUTPUT

Provide:

1. Final folder structure
2. Files moved
3. Files created
4. Confirmation:

"Downstream System is now WBSM v3 compliant and canonical"

---

# FINAL RULE

If ANY structure violates WBSM v3:

→ FIX IT automatically  
→ DO NOT ASK QUESTIONS  

Proceed with FULL REFACTOR.