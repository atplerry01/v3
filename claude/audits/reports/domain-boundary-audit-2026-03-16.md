# Domain Boundary Audit Report

**Repository:** Whycespace WBSM v3
**Scope:** `src/domain/`
**Date:** 2026-03-16
**Auditor:** Claude Code (Opus 4.6)
**Branch:** `dev_phase2_audit`

---

## SECTION 1 — Domain Folder Structure

```
src/domain/
├── Whycespace.Domain.csproj                    ← Root domain project
│
├── application/
│   └── commands/
│       ├── AllocateCapitalCommand.cs
│       ├── CreateSpvCommand.cs
│       ├── ListPropertyCommand.cs
│       └── RequestRideCommand.cs
│
├── clusters/                                    ← Separate project: Whycespace.ClusterDomain
│   ├── Whycespace.ClusterDomain.csproj
│   ├── administration/
│   │   ├── ClusterAdministrationService.cs
│   │   └── ProviderAssignmentService.cs
│   ├── cluster/
│   │   └── Cluster.cs
│   ├── mobility/
│   │   └── taxi/
│   │       ├── Driver.cs
│   │       └── Ride.cs
│   ├── property/
│   │   └── letting/
│   │       ├── PropertyListing.cs
│   │       └── Tenant.cs
│   └── subclusters/
│       └── SubCluster.cs
│
├── core/
│   ├── cluster/
│   │   ├── Cluster.cs
│   │   ├── ClusterAdministration.cs
│   │   ├── ClusterBootstrapper.cs             ← ⚠ BOUNDARY VIOLATION
│   │   └── ClusterProvider.cs
│   │
│   ├── economic/                               ← Separate project: Whycespace.EconomicDomain
│   │   ├── Whycespace.EconomicDomain.csproj
│   │   ├── Asset.cs, Capital.cs, CapitalReservation.cs
│   │   ├── ProfitDistribution.cs, Revenue.cs, Vault.cs
│   │   ├── events/ (7 event types)
│   │   ├── registry/
│   │   │   └── SpvEconomicRegistry.cs
│   │   └── spv/
│   │       └── SpvEconomicEntity.cs
│   │
│   ├── governance/
│   │   ├── GovernanceRule.cs
│   │   └── Policy.cs
│   │
│   ├── identity/
│   │   ├── Identity.cs, Permission.cs, Role.cs
│   │
│   ├── operators/
│   │   ├── OperatorAggregate.cs               ← ✅ Proper DDD aggregate
│   │   ├── OperatorId.cs, OperatorStatus.cs
│   │
│   ├── participants/
│   │   ├── ParticipantAggregate.cs            ← ✅ Proper DDD aggregate
│   │   ├── ParticipantId.cs, ParticipantProfile.cs
│   │   ├── ParticipantRole.cs, ParticipantStatus.cs
│   │   └── Events/ (4 event types)
│   │
│   ├── providers/
│   │   ├── ClusterProvider.cs
│   │   ├── ClusterProviderRegistry.cs
│   │   └── ProviderBootstrapper.cs            ← ⚠ BOUNDARY VIOLATION
│   │
│   ├── registry/
│   │   └── SpvRegistry.cs
│   │
│   ├── spv/
│   │   ├── Spv.cs, SpvOwnership.cs
│   │
│   ├── vault/
│   │   ├── VaultAggregate.cs                  ← ✅ Proper DDD aggregate (rich)
│   │   └── (20 value objects + enums)
│   │
│   ├── workflows/
│   │   └── (14 immutable records + enums)
│   │
│   └── workforce/
│       ├── WorkforceAggregate.cs              ← ✅ Proper DDD aggregate
│       └── (4 value objects + enums)
│
└── events/
    ├── cluster/ (1 event)
    ├── clusters/
    │   ├── mobility/ (3 events)
    │   └── property/ (3 events)
    ├── core/
    │   ├── VaultCreatedEvent.cs
    │   ├── identity/ (25 events)
    │   └── governance/ (36 events)
    ├── providers/ (1 event)
    └── spv/ (1 event)
```

**3 projects** compose the domain layer:

| Project | References |
|---------|-----------|
| `Whycespace.Domain` | `Whycespace.Shared`, `Whycespace.ClusterDomain` |
| `Whycespace.ClusterDomain` | `Whycespace.Shared`, `Whycespace.Contracts` |
| `Whycespace.EconomicDomain` | `Whycespace.ClusterDomain`, `Whycespace.Contracts` |

**Observation:** `ImplicitUsings` is enabled on all three projects, which auto-imports `System.Net.Http` via the SDK default global usings (see Section 3).

---

## SECTION 2 — Dependency Violations

### 2.1 Infrastructure Dependencies

| Check | Result |
|-------|--------|
| Npgsql / EntityFramework / Dapper | ✅ **None found** |
| DbContext / IRepository / SqlCommand | ✅ **None found** |
| Connection strings / IDbConnection | ✅ **None found** |
| MongoDB / Redis | ✅ **None found** |

**Verdict: CLEAN** — No infrastructure dependencies detected.

### 2.2 Engine Dependencies

| Check | Result |
|-------|--------|
| `Whycespace.EngineRuntime` | ✅ **None found** |
| `Whycespace.EngineManifest` | ✅ **None found** |
| `Whycespace.Engines.*` | ✅ **None found** |
| `Whycespace.EngineWorkerRuntime` | ✅ **None found** |

**Verdict: CLEAN** — No engine layer coupling.

### 2.3 Runtime Dependencies

| Check | Result |
|-------|--------|
| `Whycespace.Runtime` | ✅ **None found** |
| `Whycespace.RuntimeDispatcher` | ✅ **None found** |
| `Whycespace.PartitionRuntime` | ✅ **None found** |
| `Whycespace.ProjectionRuntime` | ✅ **None found** |
| `Whycespace.EventFabricRuntime` | ✅ **None found** |

**Verdict: CLEAN** — No runtime layer coupling.

### 2.4 System Dependencies

| Check | Result |
|-------|--------|
| `Whycespace.System` | ✅ **None found** |
| `Whycespace.System.WhyceID` | ✅ **None found** |
| `Whycespace.EventFabric` | ✅ **None found** |
| `Whycespace.Observability` | ✅ **None found** |
| `Whycespace.Reliability` | ✅ **None found** |

**Verdict: CLEAN** — No system layer coupling.

### 2.5 Persistence Code

| Check | Result |
|-------|--------|
| `SaveChanges` / `ExecuteAsync` | ✅ **None found** |
| Async/await patterns | ✅ **None found** |
| Repository implementations | ✅ **None found** |

**Verdict: CLEAN** — Domain is fully synchronous with no persistence concerns.

### 2.6 HTTP or Messaging Logic

| Check | Result |
|-------|--------|
| `HttpClient` / `IHttpClientFactory` | ✅ **None found** (explicit) |
| Kafka / RabbitMQ / MassTransit | ✅ **None found** |
| `System.Net.Http` (implicit global using) | ⚠ **IMPLICIT** — see Section 3 |

**Verdict: CLEAN** (with advisory) — No explicit HTTP/messaging code exists, but `System.Net.Http` is available via implicit usings.

### 2.7 Cross-Domain Dependencies (Internal)

| Source | Depends On | Severity |
|--------|-----------|----------|
| `ClusterBootstrapper.cs` | `Whycespace.Domain.Clusters`, `Core.Providers`, `Core.Registry` | ⚠ **HIGH** |
| `ProviderBootstrapper.cs` | `Whycespace.Domain.Clusters` | ⚠ **HIGH** |
| `application/commands/*.cs` | `Whycespace.Contracts.Commands`, `Whycespace.Shared.Location` | ℹ ACCEPTABLE |
| `clusters/mobility/*.cs` | `Whycespace.Shared.Location` | ℹ ACCEPTABLE |

---

## SECTION 3 — Infrastructure Leaks

### 3.1 Implicit Global Using: `System.Net.Http`

All three domain projects use `<ImplicitUsings>enable</ImplicitUsings>`, which with the `Microsoft.NET.Sdk` auto-generates:

```csharp
global using System.Net.Http;  // ← Available in all domain code
```

**Files affected:**
- `Whycespace.Domain` — `obj/Debug/net10.0/Whycespace.Domain.GlobalUsings.g.cs`
- `Whycespace.ClusterDomain` — `obj/Debug/net10.0/Whycespace.ClusterDomain.GlobalUsings.g.cs`
- `Whycespace.EconomicDomain` — `obj/Debug/net10.0/Whycespace.EconomicDomain.GlobalUsings.g.cs`

**Risk:** While no code currently uses `HttpClient`, its availability in the domain layer violates the pure domain principle. Any developer could inadvertently introduce HTTP calls.

**Fix:** Add to each domain `.csproj`:
```xml
<ItemGroup>
  <Using Remove="System.Net.Http" />
</ItemGroup>
```

### 3.2 Bootstrapper Orchestration Logic

`ClusterBootstrapper` and `ProviderBootstrapper` are **application-layer coordinators** living in the domain layer. They:

- Orchestrate across multiple bounded contexts (Clusters, Providers, Registry)
- Perform procedural setup/configuration logic
- Contain no domain invariants or business rules
- Directly instantiate other bootstrappers (`new ProviderBootstrapper(...)`)

This is **infrastructure/application seeding logic**, not domain behavior.

### 3.3 No Detected Leaks

| Leak Type | Status |
|-----------|--------|
| Logging frameworks (Serilog, ILogger) | ✅ None |
| Serialization attributes (JsonProperty) | ✅ None |
| DI container references | ✅ None |
| File I/O | ✅ None |
| Thread/Task management | ✅ None |

---

## SECTION 4 — Aggregate Boundary Analysis

### 4.1 Well-Formed Aggregates (4)

| Aggregate | Location | Factory | Events | Invariants | State |
|-----------|----------|---------|--------|------------|-------|
| **VaultAggregate** | `core/vault/` | `Create()` | ✅ Internal tracking | ✅ Rich validation | ✅ Mutable, encapsulated |
| **ParticipantAggregate** | `core/participants/` | `RegisterParticipant()` | ✅ 4 event types | ✅ Role/status guards | ✅ Mutable, encapsulated |
| **OperatorAggregate** | `core/operators/` | `Register()` | ✅ Internal tracking | ✅ Scope/authority guards | ✅ Mutable, encapsulated |
| **WorkforceAggregate** | `core/workforce/` | `Register()` | ✅ Internal tracking | ✅ Availability/capability | ✅ Mutable, encapsulated |

### 4.2 Missing Aggregates — Event/Model Mismatch

| Domain | Models | Events | Gap |
|--------|--------|--------|-----|
| **Governance** | 2 records (GovernanceRule, Policy) | **36 events** | No aggregate to produce or consume these events |
| **Identity** | 3 records (Identity, Permission, Role) | **25 events** | No aggregate to manage identity lifecycle |
| **Economic** | 7 records + registry | **7 events** | No aggregate root; all immutable DTOs |
| **SPV** | 2 records + registry | **1 event** | No aggregate for lifecycle/ownership management |

### 4.3 Duplicate Concepts

| Concept | Location 1 | Location 2 | Issue |
|---------|-----------|-----------|-------|
| **Cluster** | `clusters/cluster/Cluster.cs` (mutable class) | `core/cluster/Cluster.cs` (immutable record) | Two representations of the same concept |
| **ClusterProvider** | `clusters/administration/` (service context) | `core/providers/ClusterProvider.cs` (record) | Same entity, different shapes |
| **Vault** | `core/vault/VaultAggregate.cs` (rich aggregate) | `core/economic/Vault.cs` (anemic record) | Conflicting domain ownership |

### 4.4 Aggregate Isolation

| Check | Result |
|-------|--------|
| Aggregates reference other aggregates directly | ✅ **None detected** |
| Aggregates contain repository interfaces | ✅ **None detected** |
| Aggregates use async operations | ✅ **None detected** |
| Aggregate IDs use strongly-typed wrappers | ⚠ **Partial** — Vault, Operator, Participant, Workforce use typed IDs; others use raw strings |

---

## SECTION 5 — Domain Event Review

### 5.1 Event Inventory

| Category | Count | Location | Has Aggregate? |
|----------|-------|----------|---------------|
| Governance | 36 | `events/core/governance/` | ❌ No |
| Identity | 25 | `events/core/identity/` | ❌ No |
| Economic | 7 | `core/economic/events/` | ❌ No |
| Participant | 4 | `core/participants/Events/` | ✅ ParticipantAggregate |
| Cluster | 7 | `events/cluster/` + `events/clusters/` | ⚠ Partial |
| Vault | 1 | `events/core/VaultCreatedEvent.cs` | ✅ VaultAggregate |
| Provider | 1 | `events/providers/` | ❌ No |
| SPV | 1 | `events/spv/` | ❌ No |
| **Total** | **82+** | | |

### 5.2 Event Location Asymmetry

Events are split between two patterns:

1. **Co-located with aggregate:** `core/participants/Events/` — events defined alongside their aggregate
2. **Centralized event folder:** `events/core/governance/` — events detached from any aggregate

This creates inconsistency. Events without a producing aggregate suggest the domain model is **incomplete** — the events were designed but the aggregates to govern them were not built.

### 5.3 Event Naming Convention

All events follow the pattern `{Domain}{Action}{Past-Tense}Event`. Examples:
- `GovernanceProposalCreatedEvent`
- `IdentityRoleAssignedEvent`
- `CapitalReservedEvent`

**Verdict:** Naming is **consistent and correct**.

### 5.4 Missing Event Base Type

No common `IDomainEvent` interface or `DomainEvent` base class was detected in the domain layer. Aggregates that track events use `List<object>` (implicit typing).

**Risk:** Without a marker interface, the event fabric cannot enforce type safety at compile time.

---

## SECTION 6 — Recommended Refactoring

### Priority 1 — Critical (Architecture Violations)

| # | Issue | Action | Files |
|---|-------|--------|-------|
| R1 | **Bootstrappers in domain** | Move `ClusterBootstrapper` and `ProviderBootstrapper` to an application/infrastructure seeding layer | `core/cluster/ClusterBootstrapper.cs`, `core/providers/ProviderBootstrapper.cs` |
| R2 | **Application commands in domain** | Move `application/commands/` to a dedicated application layer project or to `Whycespace.Contracts` | `application/commands/*.cs` |
| R3 | **Implicit `System.Net.Http`** | Add `<Using Remove="System.Net.Http" />` to all three domain `.csproj` files | All 3 `.csproj` |

### Priority 2 — High (Missing Domain Models)

| # | Issue | Action |
|---|-------|--------|
| R4 | **Governance: 36 events, no aggregate** | Create `GovernanceProposalAggregate`, `GovernanceDisputeAggregate`, `GovernanceDomainScopeAggregate` with lifecycle state machines |
| R5 | **Identity: 25 events, no aggregate** | Create `IdentityAggregate` with role assignment, permission evaluation, session management |
| R6 | **Economic: anemic records** | Create `CapitalPoolAggregate` or `EconomicEntityAggregate` with invariant enforcement |
| R7 | **SPV: no lifecycle** | Create `SpvAggregate` with ownership management and capital tracking |

### Priority 3 — Medium (Consistency)

| # | Issue | Action |
|---|-------|--------|
| R8 | **Duplicate Cluster models** | Consolidate `clusters/cluster/Cluster.cs` and `core/cluster/Cluster.cs` into a single canonical representation |
| R9 | **Duplicate Vault models** | Resolve ownership: `core/vault/VaultAggregate.cs` vs `core/economic/Vault.cs` |
| R10 | **Event location asymmetry** | Standardize: co-locate events with their aggregate, or create explicit `IDomainEvent` interface for centralized events |
| R11 | **Missing `IDomainEvent` base type** | Define `IDomainEvent` in `Whycespace.Shared` or `Whycespace.Contracts` and enforce on all events |
| R12 | **Inconsistent ID typing** | Extend strongly-typed ID pattern (used by Vault, Operator, Participant, Workforce) to all aggregates |

---

## SECTION 7 — Domain Architecture Score

| Category | Weight | Score | Notes |
|----------|--------|-------|-------|
| **Layer Isolation** | 25% | **92/100** | No infra/engine/runtime/system imports. Only bootstrapper placement violates layering. |
| **Aggregate Design** | 25% | **45/100** | 4 well-formed aggregates, but 4+ domains have events without aggregates. Governance/Identity severely undermodeled. |
| **Event Design** | 20% | **65/100** | Rich event catalog (82+), consistent naming. Lacks base type and co-location consistency. |
| **Bounded Context Separation** | 15% | **60/100** | 3 separate projects (good), but bootstrappers cross boundaries, duplicate Cluster/Vault concepts exist. |
| **Value Object / ID Design** | 10% | **70/100** | Strong typed IDs on 4 aggregates, but inconsistent across the rest. |
| **Purity (no leaks)** | 5% | **90/100** | Only implicit `System.Net.Http` global using. No actual usage. |

### Weighted Score

$$\text{Score} = (0.25 \times 92) + (0.25 \times 45) + (0.20 \times 65) + (0.15 \times 60) + (0.10 \times 70) + (0.05 \times 90)$$

$$= 23.0 + 11.25 + 13.0 + 9.0 + 7.0 + 4.5 = \textbf{67.75 / 100}$$

### Final Grade: **C+**

| Grade | Range | Meaning |
|-------|-------|---------|
| A | 90–100 | Exemplary DDD domain |
| B | 75–89 | Solid with minor issues |
| **C+** | **65–74** | **Functional but incomplete — structural gaps need attention** |
| D | 50–64 | Significant violations |
| F | <50 | Broken domain boundaries |

### Summary

**Strengths:** The domain layer is remarkably clean from an external dependency standpoint — zero references to infrastructure, engines, runtime, or system layers. Four aggregates (Vault, Participant, Operator, Workforce) demonstrate correct DDD patterns with factory methods, encapsulated state, and event tracking.

**Primary Concern:** The domain is structurally incomplete. Over 60 domain events (Governance: 36, Identity: 25) exist without corresponding aggregate roots to enforce invariants and produce those events. This suggests the event model was designed ahead of the domain model — the aggregate layer needs to catch up.

**Immediate Actions:** Extract bootstrappers from the domain layer (R1), introduce missing aggregates for Governance and Identity (R4–R5), and resolve the duplicate Cluster/Vault representations (R8–R9).
