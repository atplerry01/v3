# WHYCESPACE WBSM v3 — CLUSTER RUNTIME STANDARD

Status: LOCKED
Version: WBSM v3
Scope: Cluster Architecture & Runtime Integration
Companions: [architecture-lock.md](architecture-lock.md), [implementation-guardrails.md](implementation-guardrails.md)

---

## 1. CLUSTER POSITION IN WHYCESPACE

Clusters are economic execution environments operating in the **downstream** layer:

| Layer      | Systems                                    |
|------------|--------------------------------------------|
| Upstream   | WhycePolicy, WhyceID, WhyceChain           |
| Midstream  | WSS (Workflow System), WhyceAtlas, WhycePlus |
| Downstream | Clusters, Providers, SPVs, Economic, Assets |

---

## 2. CLUSTER HIERARCHY

Each cluster follows the canonical hierarchy:

```
Cluster -> Authority -> SubCluster -> SPV
```

Example — WhyceProperty:

```
WhyceProperty
    AcquisitionAuthority
        AcquisitionSPV
    LettingAuthority
        LettingSPV
    MaintenanceAuthority
        MaintenanceSPV
```

Clusters must follow the Whycespace cluster taxonomy.

---

## 3. CLUSTER REPOSITORY LOCATIONS

| Content              | Location                           | Layer   |
|----------------------|------------------------------------|---------|
| Cluster runtime models | `src/system/clusters/`           | System  |
| Cluster domain models  | `src/domain/clusters/`           | Domain  |
| Cluster engines        | `src/engines/T2E/`              | Engines |
| Cluster workflows      | `src/runtime/workflows/`        | Runtime |
| Cluster projections    | `src/runtime/projections/clusters/` | Runtime |

Domain models must **never** exist inside runtime or engines.

For layer placement rules, see [implementation-guardrails.md](implementation-guardrails.md) sections 4-5.

---

## 4. CLUSTER EXAMPLES ACROSS DOMAINS

| Cluster       | Workflow                     | Engine                   | Event              | Projection                |
|---------------|------------------------------|--------------------------|--------------------|---------------------------|
| WhyceProperty | PropertyAcquisitionWorkflow  | PropertyAcquisitionEngine | PropertyAcquired  | PropertyListingProjection |
| WhyceMobility | RideRequestWorkflow          | RideDispatchEngine       | RideRequested      | DriverLocationProjection  |
| WhyceEnergy   | EnergyProductionWorkflow     | EnergyOutputEngine       | EnergyGenerated    | —                         |

All engines must follow [engine-implementation-standard.md](engine-implementation-standard.md).
All events must follow [event-fabric-kafka-standard.md](event-fabric-kafka-standard.md).
All projections must follow [projection-read-model-standard.md](projection-read-model-standard.md).

---

## 5. CLUSTER QUERY SERVICES

Cluster read models are exposed through query services:

| Service              | Cluster  |
|----------------------|----------|
| PropertyQueryService | Property |
| RideQueryService     | Mobility |
| EnergyQueryService   | Energy   |

Rules:

- Queries must **not** invoke engines
- Queries must only read projections

---

## 6. ECONOMIC INTEGRATION

Clusters integrate with the economic lifecycle:

```
Vault -> Capital -> SPV -> Asset -> Revenue -> Profit Distribution
```

Clusters must emit economic events:

| Event                        | Stage               |
|------------------------------|---------------------|
| CapitalContributionRecorded  | Capital allocation  |
| AssetRegistered              | Asset registration  |
| RevenueRecorded              | Revenue tracking    |
| ProfitDistributed            | Profit distribution |

---

## 7. CLUSTER REGISTRY & DISCOVERY

Clusters must be registered in the cluster registry:

| Field              | Purpose                    |
|--------------------|----------------------------|
| ClusterId          | Unique cluster identifier  |
| ClusterName        | Display name               |
| Authorities        | Authority list             |
| SupportedWorkflows | Available workflows        |
| SupportedEngines   | Available engines          |

---

## 8. CLUSTER EXTENSIBILITY

Clusters must allow new authorities and SPVs without breaking architecture:

```
WhyceProperty (existing)
    + RenovationAuthority (new)
        RenovationSPV (new)
```

New authorities must follow the existing cluster hierarchy and comply with all canonical standards.

---

## 9. CLUSTER OBSERVABILITY

| Metric                  | Purpose                   |
|-------------------------|---------------------------|
| `cluster_throughput`    | Operations processed/sec  |
| `cluster_errors`        | Error count               |
| `cluster_latency`       | End-to-end latency        |
| `workflow_success_rate`  | Workflow completion rate  |

For full observability layer, see [architecture-lock.md](architecture-lock.md) section 15.

---

## 10. CLUSTER TESTING

Cluster implementations must include tests:

| Scenario            | Purpose                        |
|---------------------|--------------------------------|
| Workflow execution  | End-to-end workflow validation |
| Engine invocation   | Engine contract compliance     |
| Event emission      | Correct events emitted         |
| Projection updates  | Read models updated correctly  |

Clusters must support **integration testing**.
