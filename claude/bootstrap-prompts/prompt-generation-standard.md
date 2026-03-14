# WHYCESPACE WBSM v3 — PROMPT GENERATION STANDARD

Status: LOCKED
Version: WBSM v3
Scope: AI-Assisted Code Generation Governance
Companions: [architecture-lock.md](architecture-lock.md), [implementation-guardrails.md](implementation-guardrails.md)

---

## 1. PURPOSE

Prompts are used to generate: engines, domain models, workflow definitions, runtime components, projections, infrastructure adapters, tests, and debug endpoints.

This standard prevents architecture drift when using AI-assisted development.

Prompts that violate these rules must not be executed.

---

## 2. CANONICAL PROMPT STRUCTURE

All prompts must follow this 6-section structure. No section may be skipped.

| Section | Name                       | Purpose                                    |
|---------|----------------------------|--------------------------------------------|
| 1       | Context                    | System environment and architecture layers |
| 2       | Architecture Rules         | Enforced constraints (reference lock docs) |
| 3       | Target Component           | Single component being implemented         |
| 4       | Implementation Requirements | Files, contracts, and constraints          |
| 5       | Validation Rules           | Build, test, and audit criteria            |
| 6       | Expected Output            | Output format and completeness rules       |

---

## 3. SECTION 1 — CONTEXT

Informs the AI about the system environment. Must reference the canonical architecture documents:

- [architecture-lock.md](architecture-lock.md)
- [implementation-guardrails.md](implementation-guardrails.md)
- [runtime-execution-model.md](runtime-execution-model.md)

---

## 4. SECTION 2 — ARCHITECTURE RULES

Must enforce the rules defined in [architecture-lock.md](architecture-lock.md) sections 3 and 19, and [implementation-guardrails.md](implementation-guardrails.md) sections 2 and 14.

Do not duplicate rules inline — reference the canonical source.

---

## 5. SECTION 3 — TARGET COMPONENT

The prompt must clearly identify the single component being implemented:

| Component Type        | Example                |
|-----------------------|------------------------|
| Execution Engine      | `VaultAllocationEngine` |
| Workflow Engine       | `PropertyAcquisitionWorkflow` |
| Projection Service    | `VaultBalanceProjection` |
| Runtime Dispatcher    | `PartitionRouter`      |
| Domain Aggregate      | `SPVAggregate`         |
| Infrastructure Adapter | `KafkaEventPublisher` |

Each prompt must implement **only ONE component**.
Prompts must never implement multiple architectural layers simultaneously.

---

## 6. SECTION 4 — IMPLEMENTATION REQUIREMENTS

Must define exactly what should be implemented.

Example structure:

```
Files:
  EngineName.cs
  EngineInput.cs
  EngineResult.cs
  EngineTests.cs

Requirements:
  - All classes must be sealed
  - All models must be immutable records
  - Engine must comply with architecture-lock.md section 3
```

---

## 7. SECTION 5 — VALIDATION RULES

Must define validation criteria:

- Build succeeds with 0 warnings, 0 errors
- All tests pass
- Architecture rules respected (per [implementation-guardrails.md](implementation-guardrails.md) section 15)

---

## 8. SECTION 6 — EXPECTED OUTPUT

Must define expected output format:

- Full code for each file
- Unit tests
- Build instructions
- Example usage

AI must not produce partial implementations.

---

## 9. PROMPT NAMING CONVENTION

Format: `phase.component.operation.prompt.md`

Examples:

| File Name                              | Meaning                      |
|----------------------------------------|------------------------------|
| `2.2.1.vault.aggregate.prompt.md`      | Phase 2, vault aggregate     |
| `2.1.10.workflow.dispatcher.prompt.md` | Phase 2, workflow dispatcher |

---

## 10. PROMPT LOCATION

Prompts must be stored in `claude/bootstrap-prompts/`. Prompts must never exist inside `src/`.

```
claude/bootstrap-prompts/
    engines/
    runtime/
    workflows/
    projections/
    infrastructure/
```

---

## 11. PROMPT EXECUTION FLOW

```
Prompt -> AI Code Generation -> Code Review -> Build Validation
  -> Test Execution -> Architecture Audit -> Commit
```

Only after passing all validation can code be committed.

---

## 12. PROMPT VERSIONING

Format: `prompt.v1.md`, `prompt.v2.md`

Prompt changes must include a change log.

---

## 13. PROMPT REVIEW PROCESS

All prompts must undergo review:

| Check                    | Verified By                  |
|--------------------------|------------------------------|
| Architecture compliance  | Lock document cross-check    |
| Security review          | No secrets in prompts        |
| Domain correctness       | Domain model validation      |
| Engine classification    | Tier placement verification  |

---

## 14. PROMPT SECURITY

Prompts must never expose:

- API keys
- Database credentials
- Private infrastructure configuration

Infrastructure secrets must come from environment configuration.

---

## 15. PROMPT TEMPLATES

Prompts should use reusable templates where possible:

| Template                      | Use Case                     |
|-------------------------------|------------------------------|
| Engine Template               | T0U-T4A engine generation    |
| Workflow Template             | Workflow definition          |
| Projection Template           | Read model generation        |
| Infrastructure Adapter Template | External integration       |

---

## 16. TEST COVERAGE REQUIREMENTS

Prompts must generate tests covering:

- Engine logic
- Failure scenarios
- Idempotency
- Retry handling

All prompts should always be placed inside markdown code containers.
