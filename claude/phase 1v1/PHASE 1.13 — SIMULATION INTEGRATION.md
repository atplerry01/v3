# WHYCESPACE WBSM v3
# PHASE 1.13.5 — SIMULATION INTEGRATION

You are implementing **Phase 1.13.5 of the Whycespace system**.

This phase integrates the **Economic Simulation Engine** with the runtime.

The simulation system allows the platform to simulate:

• cluster growth  
• SPV economic performance  
• revenue projections  
• profit distribution scenarios  

Simulations must be deterministic and must not affect live system state.

---

# OBJECTIVES

1 Integrate Simulation Engine  
2 Implement Simulation Scenario Loader  
3 Implement Simulation Runtime  
4 Connect Simulation with Cluster Domain  
5 Connect Simulation with Economic Domain  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/simulation/
```

Structure:

```
src/runtime/simulation/

runtime/
scenarios/
loader/
services/
models/
```

Create project:

```
Whycespace.SimulationRuntime.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.Domain
```

---

# SIMULATION SCENARIO MODEL

Folder:

```
models/
```

Create:

```
SimulationScenario.cs
```

Fields:

```
ScenarioId
ClusterName
SpvCount
CapitalPerSpv
DurationYears
```

Example:

```csharp
public sealed record SimulationScenario(
    Guid ScenarioId,
    string ClusterName,
    int SpvCount,
    decimal CapitalPerSpv,
    int DurationYears
);
```

---

# SIMULATION SCENARIO LOADER

Folder:

```
loader/
```

Create:

```
SimulationScenarioLoader.cs
```

Purpose:

Load simulation scenarios.

Example sources:

• configuration files  
• scenario repository  
• operator input  

Example:

```csharp
public sealed class SimulationScenarioLoader
{
    public SimulationScenario Load(Guid scenarioId)
}
```

---

# SIMULATION RUNTIME

Folder:

```
runtime/
```

Create:

```
SimulationRuntimeEngine.cs
```

Purpose:

Execute simulation scenarios.

Flow:

```
Load scenario
↓
Generate SPV models
↓
Simulate revenue growth
↓
Simulate asset value growth
↓
Generate result
```

Example method:

```
RunSimulation(SimulationScenario scenario)
```

---

# SIMULATION RESULT MODEL

Folder:

```
models/
```

Create:

```
SimulationResult.cs
```

Fields:

```
ScenarioId
ProjectedAssets
ProjectedRevenue
ProjectedProfit
```

Example:

```csharp
public sealed record SimulationResult(
    Guid ScenarioId,
    decimal ProjectedAssets,
    decimal ProjectedRevenue,
    decimal ProjectedProfit
);
```

---

# SIMULATION SERVICE

Folder:

```
services/
```

Create:

```
SimulationService.cs
```

Purpose:

Expose simulation functionality to operators.

Example methods:

```
RunScenario(Guid scenarioId)
RunClusterForecast(string clusterName)
```

---

# CLUSTER DOMAIN INTEGRATION

Simulation must be able to use cluster configuration.

Example:

```
WhyceMobility
Taxi subcluster
```

Simulate:

```
Driver count
Ride volume
Revenue
```

---

# ECONOMIC DOMAIN INTEGRATION

Simulation must use economic models.

Example:

Taxi SPV

```
Capital: £100,000
Revenue growth: 10% per year
```

---

# SAMPLE SIMULATION FLOW

Operator runs simulation.

Example:

```
Simulate 50 Taxi SPVs
```

Flow:

```
Load scenario
↓
Generate SPV models
↓
Simulate revenue
↓
Compute projected profit
↓
Return simulation result
```

---

# UNIT TESTS

Create project:

```
tests/simulation-runtime/
```

Tests:

```
SimulationScenarioTests.cs
SimulationRuntimeTests.cs
SimulationServiceTests.cs
```

Test cases:

```
load simulation scenario
execute deterministic simulation
generate correct simulation results
```

---

# DEBUG ENDPOINTS

Add endpoints.

POST

```
/dev/simulation/run
```

Run simulation scenario.

Example response:

```json
{
  "scenarioId": "123",
  "projectedRevenue": 500000
}
```

---

GET

```
/dev/simulation/scenarios
```

Return available simulation scenarios.

---

GET

```
/dev/simulation/results
```

Return previous simulation results.

---

# BUILD VALIDATION

Run:

```
dotnet build
```

Expected:

```
Build succeeded
0 warnings
0 errors
```

---

# TEST VALIDATION

Run:

```
dotnet test
```

Expected:

```
Tests:
3 passed
0 failed
```

---

# OUTPUT FORMAT

Return:

```
1 Files Created
2 Repository Tree
3 Build Result
4 Test Result
5 Debug Endpoints
```

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
3 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.13 is complete when:

• simulation engine integrated  
• cluster domain connected  
• economic domain connected  
• deterministic simulations execute  
• tests pass  
• debug endpoints respond  

End of Phase 1.13.