# WHYCESPACE WBSM v3
# PHASE 1.9.5 — EVENT REPLAY & STATE REBUILD ENGINE

You are implementing **Phase 1.9.5 of the Whycespace system**.

This phase introduces the **Event Replay & State Rebuild Engine**.

The system must support:

• replaying historical Kafka events  
• rebuilding system state from event streams  
• rebuilding projections  
• rebuilding workflow state  
• deterministic event replay  

This enables **disaster recovery and system reconstruction**.

All replay operations must remain **deterministic**.

---

# OBJECTIVES

1 Implement Event Replay Engine  
2 Implement Replay Controller  
3 Implement Projection Rebuilder  
4 Implement Workflow State Rebuilder  
5 Integrate replay with Event Consumer  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

src/runtime/event-replay/

Structure:

src/runtime/event-replay/

├── engine/
├── projections/
├── workflows/
├── controller/
└── models/

Create project:

Whycespace.EventReplay.csproj

Target framework:

net10.0

References:

Whycespace.Contracts  
Whycespace.EventFabric  
Whycespace.WorkflowRuntime  
Whycespace.Reliability  

---

# EVENT REPLAY ENGINE

Folder:

engine/

Create:

EventReplayEngine.cs

Purpose:

Replay historical events from Kafka topics.

Example:

public sealed class EventReplayEngine
{
    private readonly IEventPublisher _publisher;

    public Task ReplayTopicAsync(
        string topic,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}

Responsibilities:

• read events from Kafka  
• reprocess events  
• maintain deterministic ordering  

Replay must preserve:

PartitionKey ordering.

---

# REPLAY CONTROLLER

Folder:

controller/

Create:

ReplayController.cs

Purpose:

Manage replay execution.

Example:

public sealed class ReplayController
{
    private readonly EventReplayEngine _engine;

    public Task ReplayAllAsync(DateTime from);

    public Task ReplayTopicAsync(string topic, DateTime from);
}

---

# PROJECTION REBUILDER

Folder:

projections/

Create:

ProjectionRebuilder.cs

Purpose:

Rebuild projections from events.

Example:

public sealed class ProjectionRebuilder
{
    public Task RebuildAsync(string projectionName);
}

Responsibilities:

• reset projection state  
• replay events  
• rebuild read models  

---

# WORKFLOW STATE REBUILDER

Folder:

workflows/

Create:

WorkflowStateRebuilder.cs

Purpose:

Rebuild workflow state from events.

Example:

public sealed class WorkflowStateRebuilder
{
    public Task RebuildWorkflowsAsync();
}

Flow:

EventReplayEngine  
 ↓  
Workflow reconstruction  
 ↓  
WorkflowStateStore  

---

# REPLAY SAFETY

Replay must remain safe with idempotency guard.

Event flow during replay:

ReplayEngine  
 ↓  
EventConsumer  
 ↓  
EventProcessingGuard  
 ↓  
EventRouter  

Duplicate events must be ignored.

---

# SAMPLE REPLAY

Example:

Replay events from:

whyce.workflow.events

Time range:

2025-01-01 → 2025-01-02

Engine processes events sequentially.

---

# UNIT TESTS

Create project:

tests/event-replay/

Tests:

EventReplayEngineTests.cs  
ProjectionRebuilderTests.cs  
WorkflowStateRebuilderTests.cs  

Test cases:

replay event stream  
rebuild projections  
rebuild workflow state  

---

# DEBUG ENDPOINTS

Add endpoints:

POST

/dev/replay/topic

Body:

{
 "topic": "whyce.workflow.events",
 "from": "2025-01-01"
}

---

POST

/dev/replay/projections

Rebuild all projections.

---

POST

/dev/replay/workflows

Rebuild workflow state.

---

GET

/dev/replay/status

Return replay status.

Example:

{
 "replaying": true,
 "processedEvents": 1043
}

---

# BUILD VALIDATION

Run:

dotnet build

Expected:

Build succeeded  
0 warnings  
0 errors  

---

# TEST VALIDATION

Run:

dotnet test

Expected:

Tests:
3 passed
0 failed

---

# OUTPUT FORMAT

Return:

1 Files Created  
2 Repository Tree  
3 Build Result  
4 Test Result  
5 Debug Endpoints  

Example:

Build succeeded  
0 warnings  
0 errors  

Tests:
3 passed
0 failed

---

# PHASE COMPLETION CRITERIA

Phase 1.9.5 is complete when:

• events can be replayed from Kafka  
• projections rebuild correctly  
• workflow state rebuilds correctly  
• replay is deterministic  
• idempotency prevents duplicates  
• tests pass  
• debug endpoints respond  

End of Phase 1.9.5.