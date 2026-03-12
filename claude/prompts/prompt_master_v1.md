# WBSM v3 PROMPT STRUCTURAL RULES (MANDATORY)

You are implementing code inside the Whycespace WBSM v3 architecture.

You MUST follow the repository structure exactly.

You are NOT allowed to create new top-level directories or move layers.

If a path is unclear, STOP and request clarification.

Do NOT invent folders.

Do NOT place engines inside the system layer.

Do NOT place models inside the engine layer.

Follow the canonical structure below.

---

# CANONICAL LAYER STRUCTURE

src/

system/        → domain models, contracts, schemas
engines/       → runtime engines
runtime/       → orchestration runtime
platform/      → API, operator control plane, developer tools
infrastructure/→ database, messaging, observability

---

# ENGINE TAXONOMY

All engines must live inside:

src/engines/

Engine tiers:

T0U → Constitutional Engines
T1M → Orchestration Engines
T2E → Execution Engines
T3I → Intelligence Engines
T4A → Access Engines

Example:

src/engines/T1M/WSS/

---

# SYSTEM LAYER RULES

src/system/

Contains ONLY:

• domain models
• contracts
• DTOs
• schemas

System layer must NEVER contain:

• runtime engines
• stores
• execution logic

---

# ENGINE LAYER RULES

src/engines/

Contains:

• stateless engines
• stores
• runtime processing

Engine layer must NEVER contain:

• domain definitions
• system models

Engines may depend on system models.

System models may NOT depend on engines.

---

# WSS IMPLEMENTATION RULE

WSS is a T1M orchestration system.

Therefore:

Models must be located in:

src/system/midstream/WSS/models/

Engines must be located in:

src/engines/T1M/WSS/

Stores must be located in:

src/engines/T1M/WSS/stores/

---

# IMPORT RULE

Engines import system models:

Correct:

using Whycespace.System.Midstream.WSS.Models;

Incorrect:

System layer importing engines.

---

# FILE PLACEMENT RULE

Before generating any file, verify:

1 The file path matches the canonical architecture
2 The layer placement is correct
3 No new folder hierarchy is invented

If the correct path does not exist, create it exactly as defined.

---

# ENGINE IMPLEMENTATION RULES

All engines must be:

• stateless
• thread-safe
• deterministic

Engines must never call other engines directly.

Engines must be invoked by the runtime layer.

---

# VALIDATION RULE

Before completing the implementation:

Confirm that:

• No engine was created inside src/system/
• No models were created inside src/engines/
• The canonical architecture was respected


# STRUCTURE VALIDATION

Before generating code, print the target file structure.

Verify the structure matches the canonical repository.

If it does not match, correct it before writing code.