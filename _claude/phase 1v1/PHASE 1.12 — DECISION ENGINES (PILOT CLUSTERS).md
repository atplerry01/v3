# WHYCESPACE WBSM v3
# PHASE 1.12 — DECISION ENGINES (PILOT CLUSTERS)

You are implementing **Phase 1.12 of the Whycespace system**.

This phase introduces **Decision Engines** that power the pilot clusters.

Clusters:

WhyceMobility → Taxi  
WhyceProperty → LettingAgent  

Decision engines must:

• read projection data  
• produce deterministic decisions  
• emit events  
• remain stateless  

They must never access databases directly.

All reads must go through **ProjectionQueryService**.

---

# ENGINE CLASSIFICATION

Decision engines belong to the **T3I Intelligence layer**.

They compute decisions using projection data.

They do NOT mutate state directly.

---

# OBJECTIVES

1 Implement Mobility Decision Engines  
2 Implement Property Decision Engines  
3 Integrate ProjectionQueryService  
4 Register engines in EngineRegistry  
5 Implement unit tests  
6 Provide debug endpoints  

---

# LOCATION

Create engines under:

```
src/engines/T3I_Intelligence/
```

Structure:

```
src/engines/T3I_Intelligence/

├── mobility/
└── property/
```

---

# DOMAIN EVENTS LOCATION

Decision engines must emit **domain events**.

Events must be created under:

```
src/domain/

├── mobility/events/
└── property/events/
```

---

# MOBILITY ENGINE — DRIVER MATCHING

File:

```
DriverMatchingEngine.cs
```

Purpose:

Find nearest available driver.

Reads:

```
DriverLocationProjection
```

Produces:

```
DriverMatchedEvent
```

Example:

```csharp
public sealed class DriverMatchingEngine : IEngine
{
    private readonly ProjectionQueryService _queryService;

    public string Name => "DriverMatchingEngine";

    public DriverMatchingEngine(ProjectionQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken)
    {
        var driver = await _queryService.GetAsync("nearest-driver");

        var eventMessage = new DriverMatchedEvent(
            Guid.NewGuid(),
            context.WorkflowId,
            driver
        );

        return new EngineResult(
            true,
            new[] { eventMessage },
            null
        );
    }
}
```

---

# MOBILITY ENGINE — RIDE CREATION

File:

```
RideCreationEngine.cs
```

Purpose:

Create ride after driver match.

Produces:

```
RideCreatedEvent
```

Fields:

```
RideId
DriverId
RiderId
```

---

# MOBILITY ENGINE — RIDE COMPLETION

File:

```
RideCompletionEngine.cs
```

Purpose:

Complete ride.

Produces:

```
RideCompletedEvent
```

---

# PROPERTY ENGINE — PROPERTY LISTING

File:

```
PropertyListingEngine.cs
```

Purpose:

Create property listing.

Produces:

```
PropertyListingCreatedEvent
```

Fields:

```
PropertyId
Address
ListingStatus
```

---

# PROPERTY ENGINE — TENANT MATCHING

File:

```
TenantMatchingEngine.cs
```

Purpose:

Match tenant with property.

Reads:

```
PropertyListingProjection
```

Produces:

```
TenantMatchedEvent
```

---

# PROPERTY ENGINE — LEASE CREATION

File:

```
LeaseCreationEngine.cs
```

Purpose:

Create lease agreement.

Produces:

```
LeaseCreatedEvent
```

Fields:

```
LeaseId
TenantId
PropertyId
```

---

# ENGINE REGISTRATION

Update:

```
EngineBootstrapper
```

Register engines:

```
DriverMatchingEngine
RideCreationEngine
RideCompletionEngine
PropertyListingEngine
TenantMatchingEngine
LeaseCreationEngine
```

---

# SAMPLE WORKFLOWS

TaxiRideRequestWorkflow

Steps:

```
RideRequestValidationEngine
DriverMatchingEngine
RideCreationEngine
RideCompletionEngine
```

---

PropertyLettingWorkflow

Steps:

```
PropertyListingEngine
TenantMatchingEngine
LeaseCreationEngine
```

---

# UNIT TESTS

Create project:

```
tests/decision-engines/
```

Tests:

```
DriverMatchingEngineTests.cs
RideCreationEngineTests.cs
TenantMatchingEngineTests.cs
LeaseCreationEngineTests.cs
```

Test cases:

```
projection query usage
event emission
engine determinism
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/engines/t3i
```

Return registered decision engines.

Example:

```json
{
  "decisionEngines": [
    "DriverMatchingEngine",
    "RideCreationEngine",
    "RideCompletionEngine",
    "PropertyListingEngine",
    "TenantMatchingEngine",
    "LeaseCreationEngine"
  ]
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
5 Debug Endpoints
```

---

# PHASE COMPLETION CRITERIA

Phase 1.12 is complete when:

• decision engines compile  
• engines read projections  
• engines emit events  
• mobility workflow executes  
• property workflow executes  
• tests pass  
• debug endpoints respond  

End of Phase 1.12.