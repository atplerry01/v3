# WHYCESPACE WBSM v3
# PHASE 1.17 — END-TO-END VALIDATION

You are implementing **Phase 1.17 of the Whycespace system**.

This phase validates the **complete system pipeline**.

All components built in previous phases must work together.

System pipeline:

API Gateway (Platform)
↓
T4A Access Engines
↓
Command System
↓
Runtime Dispatcher
↓
Workflow Runtime
↓
Execution Engines
↓
Global Event Fabric
↓
Projection System
↓
Economic Runtime
↓
Cluster Domain

This phase confirms that **the system operates end-to-end**.

---

# OBJECTIVES

1 Validate API → Command pipeline  
2 Validate workflow execution  
3 Validate engine invocation  
4 Validate event publishing  
5 Validate projection updates  
6 Validate economic runtime  
7 Validate pilot cluster flows  
8 Implement integration tests  
9 Provide validation endpoints  

---

# LOCATION

Create module:

```
src/runtime/validation/
```

Structure:

```
src/runtime/validation/

models/
scenarios/
runners/
pipelines/
reports/
```

Create project:

```
Whycespace.RuntimeValidation.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.AccessEngines
Whycespace.CommandSystem
Whycespace.RuntimeDispatcher
Whycespace.EventFabric
Whycespace.Projections
Whycespace.EconomicDomain
```

---

# VALIDATION SCENARIO MODEL

Folder:

```
models/
```

Create:

```
ValidationScenario.cs
```

Fields:

```
ScenarioId
ScenarioName
ClusterName
Description
```

Example:

```csharp
public sealed record ValidationScenario(
    Guid ScenarioId,
    string ScenarioName,
    string ClusterName,
    string Description
);
```

---

# VALIDATION RUNNER

Folder:

```
runners/
```

Create:

```
ValidationRunner.cs
```

Purpose:

Execute validation scenarios.

Example method:

```
RunScenario(Guid scenarioId)
```

Flow:

```
Trigger API request
↓
CommandDispatcher
↓
Workflow Runtime
↓
Execution Engines
↓
Events published
↓
Projections updated
↓
Validate outputs
```

---

# MOBILITY VALIDATION SCENARIO

Folder:

```
scenarios/
```

Create:

```
TaxiRideScenario.cs
```

Scenario:

Taxi ride request.

Steps:

```
RequestRideCommand
DriverMatchingEngine
RideCreationEngine
RideCompletedEvent
RevenueRecordingEngine
ProfitDistributionEngine
```

Expected results:

```
Ride recorded
Revenue recorded
Profit distributed
```

---

# PROPERTY VALIDATION SCENARIO

Create:

```
PropertyLettingScenario.cs
```

Scenario:

Property letting.

Steps:

```
CreatePropertyListingCommand
TenantMatchingEngine
LeaseCreationEngine
RevenueRecordingEngine
ProfitDistributionEngine
```

Expected results:

```
Lease created
Revenue recorded
Profit distributed
```

---

# EVENT FLOW VALIDATION

Ensure event flow works.

Pipeline:

```
Workflow
↓
EngineResult
↓
EventPublisher
↓
Kafka Topic
↓
ProjectionEngine
↓
Projection Store
```

Events validated:

```
DriverMatchedEvent
RideCreatedEvent
LeaseCreatedEvent
RevenueRecordedEvent
```

---

# PROJECTION VALIDATION

Verify projections update correctly.

Examples:

```
RideStatusProjection
PropertyListingProjection
VaultBalanceProjection
```

Example validation:

```
ProjectionQueryService.Get("ride-status")
```

---

# ECONOMIC VALIDATION

Validate economic flow.

Taxi SPV revenue flow:

```
RideCompletedEvent
↓
RevenueRecordingEngine
↓
RevenueRecordedEvent
↓
ProfitDistributionEngine
↓
ProfitDistributedEvent
```

Verify:

```
Revenue recorded
Profit distributed
```

---

# INTEGRATION TESTS

Create project:

```
tests/system-validation/
```

Tests:

```
ApiPipelineTests.cs
WorkflowExecutionTests.cs
ProjectionValidationTests.cs
EconomicFlowTests.cs
```

Test cases:

```
Execute Taxi ride workflow
Execute Property letting workflow
Verify event publishing
Verify projection updates
```

---

# VALIDATION REPORT

Folder:

```
reports/
```

Create:

```
ValidationReport.cs
```

Fields:

```
ScenarioId
Success
ExecutionTime
Errors
```

Example:

```csharp
public sealed record ValidationReport(
    Guid ScenarioId,
    bool Success,
    TimeSpan ExecutionTime,
    string? Errors
);
```

---

# DEBUG ENDPOINTS

Add endpoints.

POST

```
/dev/validation/run
```

Run all validation scenarios.

Example response:

```
{
 "status": "validation started"
}
```

---

GET

```
/dev/validation/results
```

Return validation reports.

Example:

```
{
 "scenarios": [
  {
   "name": "TaxiRideScenario",
   "success": true
  },
  {
   "name": "PropertyLettingScenario",
   "success": true
  }
 ]
}
```

---

GET

```
/dev/validation/pipeline
```

Return runtime pipeline status.

Example:

```
{
 "api": "ok",
 "commands": "ok",
 "workflows": "ok",
 "engines": "ok",
 "events": "ok",
 "projections": "ok"
}
```

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
4 passed
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
5 Validation Results
6 Debug Endpoints
```

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed

Validation:

TaxiRideScenario → success
PropertyLettingScenario → success
```

---

# PHASE COMPLETION CRITERIA

Phase 1.17 is complete when:

• API triggers commands successfully  
• workflows execute correctly  
• engines produce events  
• events publish to Kafka  
• projections update correctly  
• economic flows execute correctly  
• pilot clusters function end-to-end  
• tests pass  
• validation scenarios succeed  

End of Phase 1.17.