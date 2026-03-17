# CLAUDE CODE PROMPT
# WHYCESPACE WBSM v3 — DOMAIN BOUNDARY GUARD IMPLEMENTATION

You are working inside the **Whycespace WBSM v3 architecture**.

Your task is to implement **Domain Boundary Guardrails** that prevent architectural drift in the repository.

These guardrails ensure that **core system aggregates, business cluster models, and services are never mixed incorrectly**.

The solution must be **non-invasive**, meaning:

• No domain logic changes  
• No runtime behavior changes  
• Only structural enforcement tools  

---

# PURPOSE

Whycespace is a **large-scale multi-layer economic infrastructure system**.

To maintain architecture integrity as the repository grows, we must enforce strict separation between:

Core System Domain  
Cluster Business Domains  
Application Logic  

Without guardrails, developers may accidentally place files in the wrong layer.

This prompt implements automated protection.

---

# CANONICAL DOMAIN STRUCTURE

The domain layer MUST follow this structure.

src/domain/

core/
    cluster/
        aggregates/
        services/
        bootstrap/

clusters/
    mobility/
    property/
    energy/
    finance/

application/
    commands/
    queries/

This structure is **architecturally locked**.

---

# GUARD RULES

Implement rules that enforce the following:

---

## RULE 1 — CORE DOMAIN PROTECTION

Directory:

src/domain/core/

Allowed content:

• system aggregates  
• system domain services  
• system bootstrap components  

Forbidden content:

• business models  
• cluster entities  
• platform logic  
• infrastructure logic  

---

## RULE 2 — CLUSTER DOMAIN PROTECTION

Directory:

src/domain/clusters/

Allowed content:

• business entities  
• business aggregates  
• cluster domain models  

Forbidden content:

• system aggregates  
• governance logic  
• identity models  

---

## RULE 3 — AGGREGATE PURITY

Directory:

src/domain/core/cluster/aggregates/

Allowed:

• aggregates  
• records  
• value objects  
• entities  

Forbidden:

• services  
• infrastructure  
• persistence  
• API code  

---

## RULE 4 — SERVICE PURITY

Directory:

src/domain/core/cluster/services/

Allowed:

• stateless domain services  

Forbidden:

• repositories  
• infrastructure access  
• external SDK calls  

---

## RULE 5 — BOOTSTRAP IS NOT BUSINESS LOGIC

Directory:

src/domain/core/cluster/bootstrap/

Bootstrap may:

• create initial cluster structures  
• configure templates  
• initialize system defaults  

Bootstrap must NOT:

• contain business rules  
• mutate aggregates directly  
• access infrastructure

---

# IMPLEMENTATION METHOD

Implement the guard using **three mechanisms**.

---

# 1 — ARCHITECTURE README

Create the following file:

docs/architecture/domain-boundary-rules.md

Document:

• domain layer rules  
• directory ownership  
• aggregate purity requirements  
• service purity requirements  

Purpose:

Educate developers before they modify domain code.

---

# 2 — DOMAIN STRUCTURE VALIDATION SCRIPT

Create a validation script:

tools/architecture/validate-domain-structure.ps1

The script must check:

• forbidden files in core domain
• services inside aggregates folder
• platform code inside domain
• infrastructure references inside domain

The script should scan:

src/domain/

and print violations.

Example output:

ARCHITECTURE VIOLATION

File:
src/domain/core/cluster/aggregates/ClusterRepository.cs

Reason:
Repositories are not allowed inside aggregates.

---

# 3 — CI ARCHITECTURE CHECK

Add a CI validation step to ensure the guard runs automatically.

Modify:

.github/workflows/build.yml

Add step:

Validate Domain Architecture

Command:

pwsh tools/architecture/validate-domain-structure.ps1

If violations exist:

The CI build must fail.

---

# VALIDATION RULES

The script must detect:

1. Repositories inside domain layer
2. Infrastructure references in domain
3. Services placed inside aggregates folder
4. Platform references inside domain
5. Duplicate aggregate definitions

---

# EXAMPLE VIOLATION DETECTION

INVALID:

src/domain/core/cluster/aggregates/ClusterRepository.cs

Reason:

Persistence is not allowed in domain aggregates.

---

INVALID:

src/domain/clusters/property/PropertyController.cs

Reason:

Controllers belong in platform layer.

---

INVALID:

src/domain/core/cluster/services/ClusterRepository.cs

Reason:

Repositories belong in infrastructure.

---

# SUCCESS CRITERIA

After implementation:

Running the validator should produce:

DOMAIN ARCHITECTURE VALIDATION PASSED

No violations detected.

---

# OUTPUT REQUIRED

After implementation provide a report.

## Domain Boundary Guard Report

Include:

Files created

docs/architecture/domain-boundary-rules.md  
tools/architecture/validate-domain-structure.ps1  

CI integration

.github/workflows/build.yml updated

Validation result

No architecture violations detected

---

# ARCHITECTURE PRINCIPLES

This repository follows **Whycespace WBSM v3 architecture**.

Core principles:

Clean Architecture  
Domain Driven Design  
Event-driven systems  
Policy-as-code governance  
Strict system boundary enforcement  

The guard must preserve these principles.

---

# EXECUTE IMPLEMENTATION