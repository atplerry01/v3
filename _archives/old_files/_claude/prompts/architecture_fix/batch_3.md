# WHYCESPACE WBSM v3
# ARCHITECTURE FIX PROMPT — BATCH 3
# DOMAIN LAYER CLEANUP

You are refactoring the Whycespace repository to restore **domain layer purity**.

The domain layer currently contains runtime bootstrap logic and infrastructure registries.

These must be removed from the domain layer.

---

# OBJECTIVE

Ensure the domain layer contains only:

• entities  
• aggregates  
• value objects  
• domain services  
• domain policies  
• domain events  

The domain layer must NOT contain:

• bootstrap logic  
• runtime initialization  
• infrastructure registries  
• persistence/state tracking  

---

# TARGET FILES

The following files currently violate domain purity.

src/domain/application/ClusterBootstrapper.cs  
src/domain/application/ProviderBootstrapper.cs  

src/domain/application/ClusterProviderRegistry.cs  
src/domain/application/SpvRegistry.cs  
src/domain/application/SpvEconomicRegistry.cs  

These must be refactored.

---

# STEP 1 — MOVE BOOTSTRAPPERS TO RUNTIME

Files:

ClusterBootstrapper.cs  
ProviderBootstrapper.cs  

Current location:

src/domain/application/

Move them to:

src/runtime/registry/

Create directory if necessary:

src/runtime/registry/

These files perform runtime initialization and should not exist in domain.

---

# STEP 2 — MOVE REGISTRIES TO RUNTIME STATE

Files:

ClusterProviderRegistry.cs  
SpvRegistry.cs  
SpvEconomicRegistry.cs  

These act as in-memory state registries.

Move them to:

src/runtime/registry/

These represent runtime state tracking.

They must not live in the domain layer.

---

# STEP 3 — UPDATE NAMESPACES

After moving files update namespaces.

Example:

Before:

namespace Whycespace.Domain.Application

After:

namespace Whycespace.Runtime.Registry

Ensure namespace hierarchy matches the new folder structure.

---

# STEP 4 — PRESERVE DOMAIN INTERFACES

If domain logic depends on these registries:

Introduce interfaces in domain.

Example:

src/domain/application/interfaces/

IClusterProviderRegistry  
ISpvRegistry  
ISpvEconomicRegistry  

Domain layer depends only on interfaces.

Runtime layer implements those interfaces.

---

# STEP 5 — REGISTER IMPLEMENTATIONS IN RUNTIME

Register implementations in runtime or infrastructure DI.

Example:

builder.Services.AddSingleton<IClusterProviderRegistry, ClusterProviderRegistry>();
builder.Services.AddSingleton<ISpvRegistry, SpvRegistry>();
builder.Services.AddSingleton<ISpvEconomicRegistry, SpvEconomicRegistry>();

---

# STEP 6 — REMOVE INFRASTRUCTURE LOGIC FROM DOMAIN

Verify no domain file contains:

ConcurrentDictionary  
Dictionary used as state store  
runtime initialization  

Domain may use collections internally for computation but must not act as a registry/state manager.

---

# STEP 7 — VALIDATE DOMAIN STRUCTURE

After refactoring confirm:

src/domain/

contains only:

application  
clusters  
core  
events  
shared  

Domain files represent only business concepts.

No runtime orchestration remains.

---

# VALIDATION CHECKLIST

Confirm the following:

No bootstrap logic inside domain  
No runtime initialization inside domain  
No in-memory registries inside domain  
Runtime state exists under src/runtime/registry  

Domain only defines business structures.

---

# EXPECTED RESULT

Build succeeds.

0 warnings  
0 errors  

All tests pass.

Domain layer becomes architecture-compliant.

---

# IMPORTANT

Do NOT modify domain business logic.

Do NOT change cluster models.

Do NOT modify aggregates or entities.

Only relocate bootstrap and registry infrastructure.

---

# END OF TASK