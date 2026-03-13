```markdown id="0g0xg4"
# WHYCESPACE WBSM v3
# PHASE 1.8 — GLOBAL EVENT FABRIC

You are implementing **Phase 1.8 of the Whycespace system**.

This phase implements the **Global Event Fabric**, which is the backbone of the distributed architecture.

The system becomes **fully event-driven**.

All workflow executions and engine results must emit events.

Events must be published to Kafka topics.

---

# OBJECTIVES

1 Implement Event Publisher
2 Implement Event Consumer
3 Define Kafka Topics
4 Implement Event Router
5 Integrate Event Publishing with Workflow Runtime
6 Implement unit tests
7 Provide debug endpoints

---

# LOCATION

Create module:

src/system/event-fabric/

Structure:

src/system/event-fabric/
├── publisher/
├── consumer/
├── router/
├── topics/
└── models/

Create project:

Whycespace.EventFabric.csproj

Target framework:

net8.0

References:

Whycespace.Contracts  
Whycespace.EngineRuntime  
Whycespace.WorkflowRuntime  

---

# KAFKA TOPICS

Create topic definitions.

Folder:

topics/

Create:

EventTopics.cs

Define topics:

whyce.commands
whyce.workflow.events
whyce.engine.events
whyce.cluster.events
whyce.spv.events
whyce.economic.events
whyce.system.events

Example:

public static class EventTopics
{
    public const string WorkflowEvents = "whyce.workflow.events";
    public const string EngineEvents = "whyce.engine.events";
    public const string ClusterEvents = "whyce.cluster.events";
    public const string EconomicEvents = "whyce.economic.events";
}

---

# EVENT PUBLISHER

Folder:

publisher/

Create interface:

IEventPublisher.cs

Example:

public interface IEventPublisher
{
    Task PublishAsync(
        string topic,
        IEvent eventMessage,
        CancellationToken cancellationToken
    );
}

---

Create implementation:

KafkaEventPublisher.cs

Use Kafka producer.

Serialize events as JSON.

Example behavior:

Publish event to topic.

---

# EVENT CONSUMER

Folder:

consumer/

Create:

KafkaEventConsumer.cs

Purpose:

Consume events from Kafka topics.

Example responsibilities:

• subscribe to topics
• deserialize events
• forward events to event router

Example:

public sealed class KafkaEventConsumer
{
    public Task StartAsync(CancellationToken cancellationToken)
}

---

# EVENT ROUTER

Folder:

router/

Create:

EventRouter.cs

Purpose:

Route events to internal handlers.

Example:

Workflow events → workflow projections  
Engine events → engine metrics  
Economic events → economic projections  

Example:

public sealed class EventRouter
{
    public Task RouteAsync(IEvent eventMessage)
}

---

# EVENT MODEL

Folder:

models/

Create:

EventEnvelope.cs

Purpose:

Wrap events with metadata.

Example:

public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    string Topic,
    object Payload,
    Timestamp Timestamp
);

---

# INTEGRATE WITH WORKFLOW RUNTIME

Modify WorkflowExecutor.

After each engine execution:

Capture EngineResult events.

Publish them through:

EventPublisher

Flow becomes:

EngineResult
↓
EventPublisher.Publish()
↓
Kafka Topic
↓
Event Consumer
↓
Event Router

---

# SAMPLE EVENT FLOW

Example workflow:

TaxiRideRequestWorkflow

Step:

DriverMatchingEngine

Produces event:

DriverMatchedEvent

Event flow:

Engine
↓
EngineResult
↓
EventPublisher
↓
Kafka topic (whyce.engine.events)
↓
Consumer
↓
EventRouter

---

# UNIT TESTS

Create project:

tests/event-fabric/

Tests:

EventPublisherTests.cs  
EventConsumerTests.cs  
EventRouterTests.cs  

Test cases:

Publish event  
Consume event  
Route event  

---

# DEBUG ENDPOINTS

Add debug endpoints.

GET /dev/events/topics

Return:

{
  "topics": [
    "whyce.workflow.events",
    "whyce.engine.events",
    "whyce.cluster.events",
    "whyce.economic.events"
  ]
}

---

POST /dev/events/publish

Accept:

EventEnvelope

Return:

"published"

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

Phase 1.8 is complete when:

• events publish to Kafka
• consumers receive events
• event router routes events
• workflow runtime emits events
• tests pass
• debug endpoints respond

End of Phase 1.8.
```
