# WHYCESPACE — WBSM v3 STRICT MODE
## T2E RESTRUCTURING (EXECUTION LAYER NORMALIZATION)

You are refactoring the T2E (Execution Tier) into a canonical enterprise structure.

⚠️ THIS IS A STRUCTURAL + ARCHITECTURAL REFACTOR
⚠️ ZERO DRIFT ALLOWED
⚠️ ALL FILES MUST BE MOVED, NORMALIZED, AND VALIDATED

---

# 🧠 OBJECTIVE

Transform current T2E into:

- Domain-driven structure
- Standardized module layout
- No duplication
- Clear separation of:
  - engines
  - models
  - adapters
  - evidence
  - registry

---

# 🧱 TARGET STRUCTURE (MANDATORY)

src/engines/T2E/

├ economic/
│   ├ capital/
│   ├ vault/
│   ├ asset/
│   ├ revenue/
│   └ distribution/
│
├ clusters/
│   ├ mobility/taxi/
│   └ property/letting/
│
├ workforce/
├ identity/
├ system/
└ shared/ (optional, minimal)

---

# 🚨 PHASE 1 — REMOVE DUPLICATION

## ❌ DELETE:

src/engines/T2E/capital/

AFTER moving contents into:

src/engines/T2E/economic/capital/

---

# 🚨 PHASE 2 — STANDARDIZE MODULE STRUCTURE

## EVERY DOMAIN MUST FOLLOW:

<domain>/
├ engines/
├ models/
├ adapters/     (if needed)
├ evidence/     (if needed)
├ registry/     (if needed)
└ tests/

---

# 🚨 PHASE 3 — FILE RELOCATION RULES

---

## 🔹 CAPITAL

MOVE:

T2E/capital/*
→ T2E/economic/capital/

Ensure structure:

economic/capital/
├ engines/
├ models/
├ adapters/
├ evidence/
├ registry/
└ tests/

---

## 🔹 VAULT

MOVE:

economic/vault/*.cs → economic/vault/engines/

economic/vault/models/* → keep in models/

economic/vault/adapters/* → keep

---

## 🔹 ASSET / REVENUE

Ensure:

economic/asset/
economic/revenue/

Have:

engines/
models/
tests/

---

## 🔹 DISTRIBUTION

Extract:

ProfitDistributionEngine.cs

→ move to:

economic/distribution/engines/

---

## 🔹 WORKFORCE (HEOS)

MOVE:

T2E/HEOS/*

→

T2E/workforce/

Split into:

workforce/
├ engines/
├ models/
└ tests/

---

## 🔹 IDENTITY

MOVE:

T2E/system/identity/

→

T2E/identity/

Preserve:

models/
engines/

---

## 🔹 SYSTEM

KEEP ONLY:

system/
├ cluster/
├ providers/

Move engines into:

engines/
models/

---

## 🔹 CLUSTERS

Normalize:

clusters/mobility/taxi/
clusters/property/letting/

Each must have:

├ engines/
├ models/
└ tests/

---

MOVE:

DriverMatchingEngine.cs → engines/
RideExecutionEngine.cs → engines/
LeaseCreationEngine.cs → engines/

---

# 🚨 PHASE 4 — MODEL CONSOLIDATION

Move ALL:

- Commands
- Results

into:

models/

Examples:

ExecuteVaultAllocationCommand.cs  
VaultAllocationResult.cs  

→

vault/models/

---

# 🚨 PHASE 5 — ADAPTER NORMALIZATION

Ensure ALL policy + identity integrations go into:

adapters/

Rename if needed:

CapitalPolicyContext → CapitalPolicyAdapter  
CapitalPolicyDecision → CapitalPolicyAdapter  

---

# 🚨 PHASE 6 — ENGINE PURITY VALIDATION

For EVERY engine:

Ensure:

- Stateless
- Implements IEngine
- No DB access
- No direct engine calls
- Emits events

---

# 🚨 PHASE 7 — NAMESPACE FIX

Update ALL namespaces to match new structure:

Example:

Whycespace.Engines.T2E.Economic.Vault.Engines

---

# 🚨 PHASE 8 — TEST STRUCTURE

Ensure:

Each domain has:

tests/

Move existing tests accordingly.

---

# 🚨 PHASE 9 — CLEANUP

DELETE EMPTY DIRECTORIES:

- T2E/capital/
- Any unused folders

---

# 🔍 VALIDATION CHECKLIST

Ensure:

✅ No duplicate domains  
✅ All engines inside /engines/  
✅ All commands/results inside /models/  
✅ All adapters inside /adapters/  
✅ No mixed files at root  
✅ Namespace consistency  
✅ Solution builds successfully  

---

# 📦 OUTPUT REQUIRED

1. Updated folder structure
2. List of moved files
3. List of deleted folders
4. Namespace updates summary
5. Build result (must succeed)

---

# 🔒 FINAL PRINCIPLE

T2E = EXECUTION DOMAIN

Structure must reflect:

- Economic system
- Cluster system
- Workforce system
- Identity execution system

NOT:
- Random engine grouping
- Flat file placement
- Mixed concerns

---

Proceed with full refactor.