# WHYCESPACE WBSM v3
# PHASE 1.10.5 — PROJECTION REBUILD ENGINE

You are implementing **Phase 1.10.5 of the Whycespace system**.

This phase introduces the **Projection Rebuild Engine**.

The Projection Rebuild Engine allows projections to be rebuilt from the event log.

This is required because Whycespace uses **event sourcing + CQRS**.

Rebuilds must be deterministic and replay events in the correct order.

---

# OBJECTIVES

1 Implement Event Log Reader  
2 Implement Projection Rebuild Engine  
3 Implement Projection Reset Mechanism  
4 Implement Projection Replay Controller  
5 Integrate rebuild with Projection Engine  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/projection-rebuild/
```

Structure:

```
src/runtime/projection-rebuild/

├── reader/
├── rebuild/
├── controller/
├── reset/
└── models/
```

Create project:

```
Whycespace.ProjectionRebuild.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.EventFabric
Whycespace.Projections
```

---

# EVENT LOG READER

Folder:

```
reader/
```

Create:

```
EventLogReader.cs
```

Purpose:

Read events from the event store or Kafka history.

Example methods:

```
ReadAllEvents()
ReadEventsByType(string eventType)
```

Example:

```csharp
public sealed class EventLogReader
{
    public IEnumerable<IEvent> ReadAllEvents()
    {
        // read events from event storage
    }
}
```

---

# PROJECTION RESET MECHANISM

Folder:

```
reset/
```

Create:

```
ProjectionResetService.cs
```

Purpose:

Reset projection state before rebuild.

Responsibilities:

```
clear projection storage
reset Redis keys
reset in-memory projection caches
```

Example:

```csharp
public sealed class ProjectionResetService
{
    public Task ResetProjection(string projectionName)
}
```

---

# PROJECTION REBUILD ENGINE

Folder:

```
rebuild/
```

Create:

```
ProjectionRebuildEngine.cs
```

Purpose:

Replay events to rebuild projections.

Example flow:

```
Reset projection
↓
Read event log
↓
Replay events
↓
Reconstruct projection state
```

Example implementation:

```csharp
public sealed class ProjectionRebuildEngine
{
    private readonly EventLogReader _reader;
    private readonly ProjectionEngine _projectionEngine;

    public async Task RebuildAsync()
    {
        var events = _reader.ReadAllEvents();

        foreach (var evt in events)
        {
            await _projectionEngine.ProcessEventAsync(evt);
        }
    }
}
```

---

# PROJECTION REPLAY CONTROLLER

Folder:

```
controller/
```

Create:

```
ProjectionReplayController.cs
```

Purpose:

Control rebuild operations.

Capabilities:

```
rebuild single projection
rebuild all projections
rebuild from checkpoint
```

Example methods:

```
RebuildProjection(string name)
RebuildAll()
```

---

# CHECKPOINT MODEL

Folder:

```
models/
```

Create:

```
ProjectionCheckpoint.cs
```

Fields:

```
ProjectionName
LastProcessedEventId
Timestamp
```

Example:

```csharp
public sealed record ProjectionCheckpoint(
    string ProjectionName,
    Guid LastProcessedEventId,
    DateTime Timestamp
);
```

---

# SAMPLE REBUILD FLOW

Example scenario:

New projection added:

```
DriverRatingProjection
```

Steps:

```
Reset projection
↓
Replay event log
↓
Projection rebuilt
↓
System resumes normal operation
```

---

# UNIT TESTS

Create project:

```
tests/projection-rebuild/
```

Tests:

```
EventLogReaderTests.cs
ProjectionResetTests.cs
ProjectionRebuildEngineTests.cs
ProjectionReplayControllerTests.cs
```

Test cases:

```
replay event log
reset projection state
rebuild projections deterministically
```

---

# DEBUG ENDPOINTS

Add endpoints.

POST

```
/dev/projections/rebuild
```

Rebuild all projections.

Example response:

```json
{
  "status": "rebuild started"
}
```

---

POST

```
/dev/projections/rebuild/{projection}
```

Rebuild a specific projection.

---

GET

```
/dev/projections/checkpoints
```

Return projection checkpoints.

Example:

```json
{
  "checkpoints": [
    {
      "projection": "DriverLocationProjection",
      "lastEvent": "abc123"
    }
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

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.10.5 is complete when:

• projections can reset safely  
• event log replay rebuilds projections  
• rebuild operations deterministic  
• checkpoints maintained  
• tests pass  
• debug endpoints respond  

End of Phase 1.10.5.