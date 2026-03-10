# WHYCESPACE WBSM v3
# PHASE 1.8.5 — EVENT SCHEMA GOVERNANCE

You are implementing **Phase 1.8.5 of the Whycespace system**.

This phase introduces **Event Schema Governance**.

Event schema governance ensures that:

• event schemas are versioned  
• event contracts remain backward compatible  
• producers cannot emit invalid events  
• consumers can validate event structure  

This phase creates the **Whycespace Event Schema Registry**.

---

# OBJECTIVES

1 Implement Event Schema Model  
2 Implement Event Schema Registry  
3 Implement Event Versioning  
4 Implement Event Schema Validator  
5 Integrate schema validation with Event Publisher  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/event-schema/
```

Structure:

```
src/runtime/event-schema/

├── models/
├── registry/
├── validation/
├── versioning/
└── enforcement/
```

Create project:

```
Whycespace.EventSchema.csproj
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

# EVENT SCHEMA MODEL

Folder:

```
models/
```

Create:

```
EventSchema.cs
```

Fields:

```
EventType
SchemaVersion
PayloadStructure
CreatedAt
```

Example:

```csharp
public sealed class EventSchema
{
    public string EventType { get; }

    public int SchemaVersion { get; }

    public IReadOnlyDictionary<string,string> PayloadStructure { get; }

    public DateTime CreatedAt { get; }

    public EventSchema(
        string eventType,
        int version,
        IReadOnlyDictionary<string,string> payload)
    {
        EventType = eventType;
        SchemaVersion = version;
        PayloadStructure = payload;
        CreatedAt = DateTime.UtcNow;
    }
}
```

---

# EVENT SCHEMA REGISTRY

Folder:

```
registry/
```

Create:

```
EventSchemaRegistry.cs
```

Purpose:

Store all event schemas.

Example storage:

```
Dictionary<string, List<EventSchema>>
```

Methods:

```
RegisterSchema
GetLatestSchema
GetSchemaVersion
```

Example:

```csharp
public sealed class EventSchemaRegistry
{
    public void RegisterSchema(EventSchema schema);

    public EventSchema GetLatest(string eventType);
}
```

---

# EVENT VERSIONING

Folder:

```
versioning/
```

Create:

```
EventVersionManager.cs
```

Purpose:

Handle schema version evolution.

Rules:

```
new schema version must not remove existing fields
fields may be added
schema must remain backward compatible
```

Methods:

```
ValidateCompatibility
IncrementVersion
```

---

# EVENT SCHEMA VALIDATOR

Folder:

```
validation/
```

Create:

```
EventSchemaValidator.cs
```

Purpose:

Validate events before publishing.

Responsibilities:

```
validate event payload fields
ensure schema version matches registry
reject invalid events
```

Example:

```csharp
public sealed class EventSchemaValidator
{
    public bool Validate(IEvent eventMessage)
}
```

---

# EVENT PUBLISHER INTEGRATION

Modify:

```
KafkaEventPublisher
```

New flow:

```
Event creation
 ↓
EventSchemaValidator
 ↓
SchemaRegistry lookup
 ↓
Publish to Kafka
```

If validation fails:

```
Reject event
```

---

# SAMPLE SCHEMA

Example:

```
DriverMatchedEvent
```

Schema:

```
DriverId: Guid
RideId: Guid
Timestamp: DateTime
```

Version:

```
1
```

---

# SCHEMA EVOLUTION

Example:

Version 1

```
DriverId
RideId
```

Version 2

```
DriverId
RideId
DriverRating
```

Allowed because:

```
field added but not removed
```

---

# UNIT TESTS

Create project:

```
tests/event-schema/
```

Tests:

```
EventSchemaTests.cs
SchemaRegistryTests.cs
SchemaValidatorTests.cs
```

Test cases:

```
register schema
validate event payload
reject invalid events
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/event-schemas
```

Return registered schemas.

Example:

```json
{
 "schemas": [
  {
   "event": "DriverMatchedEvent",
   "version": 1
  },
  {
   "event": "RevenueRecordedEvent",
   "version": 1
  }
 ]
}
```

---

GET

```
/dev/event-schemas/validate
```

Accept event payload.

Return validation result.

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

Phase 1.8.5 is complete when:

• event schemas register correctly  
• schema validation prevents invalid events  
• schema versions evolve safely  
• event publisher enforces validation  
• tests pass  
• debug endpoints respond  

End of Phase 1.8.5.