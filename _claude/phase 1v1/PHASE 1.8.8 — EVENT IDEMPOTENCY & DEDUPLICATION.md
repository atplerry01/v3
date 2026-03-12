# WHYCESPACE WBSM v3
# PHASE 1.8.8 — EVENT IDEMPOTENCY & DEDUPLICATION

You are implementing **Phase 1.8.8 of the Whycespace system**.

This phase introduces **Event Idempotency and Deduplication**.

In distributed systems, events may be delivered more than once due to:

• network retries  
• consumer restarts  
• partition rebalancing  
• replay operations  

Therefore the system must guarantee:

• events are processed exactly once  
• duplicate events are ignored  
• consumers remain deterministic  

This phase creates the **Whycespace Event Idempotency Guard**.

---

# OBJECTIVES

1 Implement Event Idempotency Model  
2 Implement Event Deduplication Registry  
3 Implement Event Processing Guard  
4 Integrate deduplication with Event Consumer  
5 Support event replay safety  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

src/runtime/event-idempotency/

Structure:

src/runtime/event-idempotency/

├── models/
├── registry/
├── guard/
├── storage/
└── enforcement/

Create project:

Whycespace.EventIdempotency.csproj

Target framework:

net10.0

References:

Whycespace.Contracts  
Whycespace.EventFabric  

---

# EVENT IDEMPOTENCY MODEL

Folder:

models/

Create:

ProcessedEvent.cs

Fields:

EventId  
EventType  
Topic  
PartitionKey  
ProcessedAt  

Example:

public sealed class ProcessedEvent
{
    public Guid EventId { get; }

    public string EventType { get; }

    public string Topic { get; }

    public string PartitionKey { get; }

    public DateTime ProcessedAt { get; }

    public ProcessedEvent(
        Guid eventId,
        string eventType,
        string topic,
        string partitionKey)
    {
        EventId = eventId;
        EventType = eventType;
        Topic = topic;
        PartitionKey = partitionKey;
        ProcessedAt = DateTime.UtcNow;
    }
}

---

# EVENT DEDUPLICATION REGISTRY

Folder:

registry/

Create:

EventDeduplicationRegistry.cs

Purpose:

Track processed events.

Storage example:

HashSet<Guid>

Example:

public sealed class EventDeduplicationRegistry
{
    private readonly HashSet<Guid> _processed = new();

    public bool HasProcessed(Guid eventId)
    {
        return _processed.Contains(eventId);
    }

    public void MarkProcessed(Guid eventId)
    {
        _processed.Add(eventId);
    }
}

---

# EVENT PROCESSING GUARD

Folder:

guard/

Create:

EventProcessingGuard.cs

Purpose:

Ensure events are processed only once.

Example:

public sealed class EventProcessingGuard
{
    private readonly EventDeduplicationRegistry _registry;

    public bool ShouldProcess(EventEnvelope envelope)
    {
        if (_registry.HasProcessed(envelope.EventId))
        {
            return false;
        }

        _registry.MarkProcessed(envelope.EventId);
        return true;
    }
}

---

# EVENT CONSUMER INTEGRATION

Modify:

KafkaEventConsumer

New flow:

Kafka consumer receives event  
 ↓
EventProcessingGuard.ShouldProcess()
 ↓
If duplicate → skip  
 ↓
If new → route event  

Example:

if (!_guard.ShouldProcess(envelope))
{
    return;
}

await _router.RouteAsync(envelope);

---

# REPLAY SUPPORT

When events are replayed:

EventProcessingGuard prevents duplicate processing.

Replay safety is guaranteed because:

EventId remains constant.

---

# SAMPLE DUPLICATE EVENT

Kafka delivers:

DriverMatchedEvent
EventId: 123

Delivery 1 → processed  
Delivery 2 → ignored  

System remains deterministic.

---

# UNIT TESTS

Create project:

tests/event-idempotency/

Tests:

EventDeduplicationRegistryTests.cs  
EventProcessingGuardTests.cs  

Test cases:

detect duplicate event  
allow first event  
reject repeated event  

---

# DEBUG ENDPOINTS

Add debug endpoints.

GET

/dev/events/deduplication

Return processed event count.

Example:

{
 "processedEvents": 152
}

---

GET

/dev/events/check/{eventId}

Return:

{
 "eventId": "123",
 "processed": true
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

Phase 1.8.8 is complete when:

• duplicate events are detected  
• duplicate events are ignored  
• event consumer remains deterministic  
• replay safety is guaranteed  
• tests pass  
• debug endpoints respond  

End of Phase 1.8.8.