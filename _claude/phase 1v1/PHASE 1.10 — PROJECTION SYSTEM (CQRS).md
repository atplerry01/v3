# WHYCESPACE WBSM v3
# PHASE 1.10 — PROJECTION SYSTEM (CQRS)

You are implementing **Phase 1.10 of the Whycespace system**.

This phase implements the **Projection System**, which builds read models from events.

The system follows **CQRS architecture**.

Write side:

Commands → Workflows → Engines → Events

Read side:

Events → Projections → Query Models

Projections subscribe to the **Global Event Fabric (Kafka)**.

---

# OBJECTIVES

1 Implement Projection Engine  
2 Implement Projection Registry  
3 Implement Projection Consumers  
4 Implement Redis Projection Cache  
5 Implement Query Service  
6 Integrate projections with Event Fabric  
7 Implement unit tests  
8 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/projections/
```

Structure:

```
src/runtime/projections/

├── engine/
├── registry/
├── consumers/
├── storage/
├── queries/
└── models/
```

Create project:

```
Whycespace.Projections.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.EventFabric
```

---

# PROJECTION ENGINE

Folder:

```
engine/
```

Create interface:

```
IProjection.cs
```

Example:

```csharp
public interface IProjection
{
    string Name { get; }

    Task HandleAsync(IEvent eventMessage);
}
```

---

Create:

```
ProjectionEngine.cs
```

Purpose:

Process events and update projections.

Example:

```csharp
public sealed class ProjectionEngine
{
    private readonly IProjectionRegistry _registry;

    public ProjectionEngine(IProjectionRegistry registry)
    {
        _registry = registry;
    }

    public async Task ProcessEventAsync(IEvent eventMessage)
    {
        var projections = _registry.Resolve(eventMessage.EventType);

        foreach (var projection in projections)
        {
            await projection.HandleAsync(eventMessage);
        }
    }
}
```

---

# PROJECTION REGISTRY

Folder:

```
registry/
```

Create interface:

```
IProjectionRegistry.cs
```

Example:

```csharp
public interface IProjectionRegistry
{
    void Register(string eventType, IProjection projection);

    IReadOnlyCollection<IProjection> Resolve(string eventType);
}
```

---

Create:

```
ProjectionRegistry.cs
```

Use:

```
Dictionary<string, List<IProjection>>
```

Purpose:

Map events → projections.

---

# PROJECTION CONSUMER

Folder:

```
consumers/
```

Create:

```
ProjectionEventConsumer.cs
```

Purpose:

Consume Kafka events and forward them to ProjectionEngine.

Example flow:

```
Kafka topic
↓
EventConsumer
↓
ProjectionEngine.ProcessEvent()
```

---

# PROJECTION STORAGE

Folder:

```
storage/
```

Create Redis-backed storage.

File:

```
RedisProjectionStore.cs
```

Responsibilities:

```
store projection state
update read models
provide fast queries
```

Example methods:

```
SetAsync
GetAsync
DeleteAsync
```

---

# QUERY SERVICE

Folder:

```
queries/
```

Create:

```
ProjectionQueryService.cs
```

Purpose:

Provide read access to projections.

Example:

```csharp
public sealed class ProjectionQueryService
{
    private readonly RedisProjectionStore _store;

    public Task<string?> GetAsync(string key);
}
```

---

# SAMPLE PROJECTIONS

Create example projections.

Location:

```
src/runtime/projections/mobility/
```

Create:

```
DriverLocationProjection.cs
```

Event handled:

```
DriverLocationUpdatedEvent
```

Projection updates:

```
driver location cache
```

---

Create:

```
RideStatusProjection.cs
```

Event handled:

```
RideCreatedEvent
RideCompletedEvent
```

Projection updates:

```
ride status model
```

---

# PROPERTY PROJECTIONS

Create:

```
PropertyListingProjection.cs
```

Handles:

```
PropertyListingCreatedEvent
```

Projection fields:

```
PropertyId
Address
Status
```

---

# ECONOMIC PROJECTIONS

Create:

```
VaultBalanceProjection.cs
```

Handles:

```
CapitalContributionRecordedEvent
ProfitDistributedEvent
```

Projection fields:

```
VaultId
Balance
```

---

# INTEGRATE WITH EVENT FABRIC

Modify:

```
EventRouter
```

New flow:

```
EventConsumer
↓
EventRouter
↓
ProjectionEngine
↓
ProjectionRegistry
↓
Projection
```

---

# SAMPLE EVENT FLOW

```
DriverLocationUpdatedEvent
↓
EventConsumer
↓
ProjectionEngine
↓
DriverLocationProjection
↓
RedisProjectionStore
```

---

# UNIT TESTS

Create project:

```
tests/projections/
```

Tests:

```
ProjectionRegistryTests.cs
ProjectionEngineTests.cs
ProjectionQueryServiceTests.cs
```

Test cases:

```
register projection
process event
update projection state
query projection
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/projections
```

Return registered projections.

Example:

```json
{
  "projections": [
    "DriverLocationProjection",
    "RideStatusProjection",
    "PropertyListingProjection",
    "VaultBalanceProjection"
  ]
}
```

---

GET

```
/dev/projections/query
```

Accept:

```
key
```

Return projection data.

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

Phase 1.10 is complete when:

• projections subscribe to events  
• projections update Redis cache  
• queries return projection data  
• tests pass  
• debug endpoints respond  

End of Phase 1.10.