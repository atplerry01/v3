# WHYCESPACE WBSM v3
# PHASE 1.20 — EVENT FABRIC RUNTIME

You are implementing **Phase 1.20 of the Whycespace system**.

This phase introduces the **Event Fabric Runtime**, which integrates the runtime with the **Whycespace Global Event Fabric (Kafka)**.

The event fabric allows the system to:

• publish domain events from execution engines  
• route events to topics  
• subscribe to event streams  
• trigger projections and downstream processes  

This phase DOES NOT modify existing engine logic.

It only introduces the **runtime event infrastructure**.

---

# ARCHITECTURE RULES

Follow WBSM v3 strictly.

1️⃣ Engines NEVER call other engines.

2️⃣ Execution engines emit **domain events**.

3️⃣ Runtime publishes events to the **Event Fabric**.

4️⃣ Event publishing must be deterministic.

5️⃣ No external libraries (Kafka client integration will come later).

6️⃣ All classes must be sealed.

---

# LOCATION

Create module:

src/runtime/event-fabric/

Structure:

src/runtime/event-fabric/

├── models/
├── publishing/
├── routing/
├── consuming/
└── registry/

Create project:

Whycespace.EventFabricRuntime.csproj

Target framework:

net10.0

Project references:

Whycespace.Contracts  
Whycespace.EngineRuntime  

---

# OBJECTIVES

Implement:

1️⃣ EventEnvelope  
2️⃣ EventSerializer  
3️⃣ EventPublisher  
4️⃣ EventTopicRouter  
5️⃣ EventConsumerRuntime  
6️⃣ EventSubscriptionRegistry  

Add unit tests.

Add debug endpoint.

---

# EVENT ENVELOPE

Create:

models/EventEnvelope.cs

Purpose:

Wraps all domain events with metadata.

Implementation:

```csharp
public sealed class EventEnvelope
{
    public string EventId { get; }

    public string EventType { get; }

    public object Payload { get; }

    public DateTime TimestampUtc { get; }

    public EventEnvelope(string eventId, string eventType, object payload)
    {
        EventId = eventId;
        EventType = eventType;
        Payload = payload;
        TimestampUtc = DateTime.UtcNow;
    }
}
```

---

# EVENT SERIALIZER

Create:

publishing/EventSerializer.cs

Purpose:

Serializes event envelopes.

Implementation:

```csharp
public sealed class EventSerializer
{
    public string Serialize(EventEnvelope envelope)
    {
        return System.Text.Json.JsonSerializer.Serialize(envelope);
    }
}
```

---

# EVENT TOPIC ROUTER

Create:

routing/EventTopicRouter.cs

Purpose:

Maps event types to Kafka topics.

Implementation:

```csharp
public sealed class EventTopicRouter
{
    private readonly Dictionary<string, string> _routes = new();

    public void Register(string eventType, string topic)
    {
        _routes[eventType] = topic;
    }

    public string ResolveTopic(string eventType)
    {
        if (!_routes.TryGetValue(eventType, out var topic))
            throw new InvalidOperationException("Event route not registered");

        return topic;
    }

    public IReadOnlyDictionary<string, string> GetRoutes()
    {
        return _routes;
    }
}
```

---

# EVENT PUBLISHER

Create:

publishing/EventPublisher.cs

Purpose:

Publishes events to the event fabric.

Implementation:

```csharp
public sealed class EventPublisher
{
    private readonly EventSerializer _serializer;
    private readonly EventTopicRouter _router;

    public EventPublisher(EventSerializer serializer, EventTopicRouter router)
    {
        _serializer = serializer;
        _router = router;
    }

    public string Publish(EventEnvelope envelope)
    {
        var topic = _router.ResolveTopic(envelope.EventType);

        var payload = _serializer.Serialize(envelope);

        return topic + ":" + payload;
    }
}
```

Note:

Actual Kafka publishing will be added in a later infrastructure phase.

---

# EVENT SUBSCRIPTION REGISTRY

Create:

registry/EventSubscriptionRegistry.cs

Purpose:

Tracks event subscriptions.

Implementation:

```csharp
public sealed class EventSubscriptionRegistry
{
    private readonly Dictionary<string, List<string>> _subscriptions = new();

    public void Subscribe(string topic, string subscriber)
    {
        if (!_subscriptions.ContainsKey(topic))
            _subscriptions[topic] = new List<string>();

        _subscriptions[topic].Add(subscriber);
    }

    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        if (!_subscriptions.TryGetValue(topic, out var list))
            return Array.Empty<string>();

        return list.AsReadOnly();
    }
}
```

---

# EVENT CONSUMER RUNTIME

Create:

consuming/EventConsumerRuntime.cs

Purpose:

Consumes events from topics.

Implementation:

```csharp
public sealed class EventConsumerRuntime
{
    private readonly EventSubscriptionRegistry _registry;

    public EventConsumerRuntime(EventSubscriptionRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        return _registry.GetSubscribers(topic);
    }
}
```

---

# DEBUG ENDPOINT

Add endpoint:

/dev/runtime/event-fabric

Returns:

• registered event routes  
• subscriber counts  

Example response:

```json
{
  "routes": {
    "CapitalContributionRecorded": "whyce.capital.events",
    "RevenueRecorded": "whyce.revenue.events"
  },
  "subscriptions": {
    "whyce.capital.events": 2,
    "whyce.revenue.events": 1
  }
}
```

---

# UNIT TESTS

Create project:

tests/runtime/event-fabric/

Add tests:

EventEnvelopeTests.cs

Verify:

• envelope creation  
• timestamp assignment  

EventTopicRouterTests.cs

Verify:

• route registration  
• topic resolution  

EventPublisherTests.cs

Verify:

• serialization  
• topic routing  

EventSubscriptionRegistryTests.cs

Verify:

• subscriber registration  
• subscriber retrieval  

---

# BUILD SUCCESS CRITERIA

Build succeeds with:

0 errors  
0 warnings  

All tests pass.

Debug endpoint returns routes and subscriptions.

---

# PHASE RESULT

After this phase the runtime can:

• wrap domain events  
• serialize events  
• route events to topics  
• register subscribers  

This prepares the system for:

PHASE 1.21 — PROJECTION RUNTIME.