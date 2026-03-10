# Whycespace Clusters

## Overview

Clusters represent economic sectors in the Whycespace system. They are located in `src/system/downstream/clusters/`.

Clusters define sector boundaries and contain sub-clusters for specific service types. Clusters do **not** contain domain models — those belong in `src/domain/`.

## Structure

```
Clusters
├── ClusterAdministration    (ClusterRegistry)
├── ClusterProviders         (WhyceMobility, WhyceProperty)
└── SubClusters              (Taxi, PropertyLetting, etc.)
```

## Registered Clusters

### WhyceMobility

| Field      | Value           |
|------------|-----------------|
| Sector     | Transportation  |
| SubClusters| taxi, logistics, fleet |

**Taxi SubCluster** — RideHailing service backed by `RideExecutionEngine` (T2E) and `DriverMatchingEngine` (T3I).

### WhyceProperty

| Field      | Value          |
|------------|----------------|
| Sector     | RealEstate     |
| SubClusters| letting, sales, management |

**PropertyLetting SubCluster** — Property letting service backed by `PropertyExecutionEngine` (T2E) and `TenantMatchingEngine` (T3I).

## SPV Management

SPVs (Special Purpose Vehicles) are managed by `SpvManager` in `src/system/downstream/spvs/`. Each SPV is associated with a cluster and tracks allocated capital.

## Economic Coordination

`EconomicCoordinator` in `src/system/downstream/economic/` records transactions and computes net positions per SPV.
