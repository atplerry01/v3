# Whycespace Engine Taxonomy

## Overview

All deterministic processing occurs inside engines at `src/engines/`. Engines are **stateless**, **sealed** classes that implement `IEngine`.

## Rules

- Engines MUST be stateless
- Engines cannot call other engines
- Engines cannot access workflow state
- Engines cannot mutate workflow directly
- Engines only process context and return results
- All orchestration is controlled by the runtime

## Tiers

### T0U — Constitutional Engines

| Engine                     | Purpose                        |
|----------------------------|--------------------------------|
| PolicyValidationEngine     | Policy constraint validation   |
| ChainVerificationEngine    | Chain integrity verification   |
| IdentityVerificationEngine | User identity verification     |

### T1M — Orchestration Engines

| Engine                  | Purpose                    |
|-------------------------|----------------------------|
| WorkflowSchedulerEngine | Workflow scheduling        |
| PartitionRouterEngine   | Partition key routing      |

### T2E — Execution Engines

| Engine                    | Purpose                           |
|---------------------------|-----------------------------------|
| RideExecutionEngine       | Ride lifecycle (validate/assign/start/complete) |
| PropertyExecutionEngine   | Property listing lifecycle        |
| EconomicExecutionEngine   | Economic lifecycle (capital/SPV/revenue/profit) |

### T3I — Intelligence Engines

| Engine                     | Purpose                    |
|----------------------------|----------------------------|
| DriverMatchingEngine       | Match drivers to rides     |
| TenantMatchingEngine       | Match tenants to listings  |
| WorkforceAssignmentEngine  | Assign workforce to tasks  |

### T4A — Access Engines

| Engine                | Purpose              |
|-----------------------|----------------------|
| AuthenticationEngine  | Token authentication |
| AuthorizationEngine   | Resource authorization |

## Exchange Contract

```
EngineInvocationEnvelope → EngineContext → IEngine.ExecuteAsync() → EngineResult
```

`EngineResult` contains: `Success`, `Events`, `Output`.
