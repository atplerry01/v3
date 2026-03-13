# WHYCESPACE WBSM v3
# IMPLEMENTATION PROMPT

You are implementing a component inside the Whycespace system.

Follow the WBSM v3 architecture strictly.

Do NOT violate the canonical repository structure.

Reference documents:

- WBSM v3 Prompt Structural Rules
- WBSM v3 Prompt Guardrails

---

# STEP 1 — ARCHITECTURE VERIFICATION

Confirm the correct layer for the component.

Possible layers:

domain
system
engines
runtime
platform
infrastructure
shared

If unsure → STOP and request clarification.

---

# STEP 2 — TARGET FILE PATH

Print the target file path before writing code.

Verify it matches the canonical architecture.

Example:

src/engines/T2E/Vault/VaultContributionEngine.cs

Confirm:

- no new top-level directories
- no layer violation

If the path is incorrect → STOP.

---

# STEP 3 — TARGET FILE STRUCTURE

Print the directory tree before generating code.

Example:

src/engines/T2E/Vault/

VaultContributionEngine.cs
VaultContributionEngineTests.cs

Confirm structure matches canonical repository.

---

# STEP 4 — DOMAIN MODEL VALIDATION

Verify required domain models exist.

Domain models must live inside:

src/domain/

If missing, define them in the domain layer first.

Domain must not depend on engines.

---

# STEP 5 — ENGINE IMPLEMENTATION

Implement the engine with the following constraints:

Engines must be:

stateless  
thread-safe  
deterministic  

Engines must NOT:

persist state  
call other engines  
contain domain entities  

Engine may depend on:

domain  
system  

Return decision models.

---

# STEP 6 — STORE (OPTIONAL)

If runtime state is required, create a store.

Stores must live inside the engine layer.

Example:

src/engines/T1M/WSS/stores/

Stores may contain:

ConcurrentDictionary  
runtime state  

Stores must NOT contain business logic.

---

# STEP 7 — RUNTIME INTEGRATION

Confirm the engine is invoked through runtime orchestration.

Correct flow:

Runtime Dispatcher → Engine

Incorrect flow:

Engine → Engine

---

# STEP 8 — UNIT TESTS

Create deterministic unit tests.

Test scenarios:

success path  
failure path  
edge cases  
concurrency safety  

All tests must pass.

---

# STEP 9 — DEBUG ENDPOINTS

Expose inspection endpoints for development.

Example:

GET /dev/workflows  
GET /dev/vaults  
GET /dev/engines  

Debug endpoints must live in:

src/platform/

---

# STEP 10 — VALIDATION CHECKLIST

Before finishing verify:

No engines inside src/system  
No models inside src/engines  
Domain isolation respected  
Cluster isolation respected  
All engines stateless  

---

# SUCCESS CRITERIA

Build succeeds  
0 warnings  
0 errors  
All tests pass