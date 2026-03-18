# WHYCESPACE WBSM v3
# PHASE 1.21 — PROJECTION RUNTIME

You are implementing **Phase 1.21 of the Whycespace system**.

This phase introduces the **Projection Runtime**, which converts event streams into **read models**.

This enables:

• CQRS read-side architecture  
• real-time dashboards  
• analytics queries  
• system observability  

Projection runtime consumes events from the **Event Fabric Runtime** and updates **projection state stores**.

---

# ARCHITECTURE RULES

Follow WBSM v3 strictly.

1️⃣ Execution engines emit domain events.

2️⃣ Event Fabric delivers events to projection workers.

3️⃣ Projections update **read models only**.

4️⃣ Projections NEVER modify domain state.

5️⃣ Projections must be deterministic.

6️⃣ All classes must be sealed.

7️⃣ No external libraries.

---

# LOCATION

Create module:

src/runtime/projections/

Structure:

src/runtime/projections/

├── models/
├── engine/
├── registry/
├── workers/
└── storage/

Create project:

Whycespace.ProjectionRuntime.csproj

Target framework:

net10.0

Project references:

Whycespace.Contracts  
Whycespace.EventFabricRuntime  

---

# OBJECTIVES

Implement:

1️⃣ ProjectionRecord  
2️⃣ ProjectionRegistry  
3️⃣ ProjectionEngine  
4️⃣ ProjectionWorker  
5️⃣ ProjectionStateStore  

Add unit tests.

Add debug endpoint.

---

# PROJECTION RECORD

Create:

models/ProjectionRecord.cs

Purpose:

Represents a projection entry.

Implementation:

```csharp
public sealed class ProjectionRecord
{
    public string ProjectionName { get; }

    public string EntityId { get; }

    public object State { get; }

    public DateTime UpdatedUtc { get; }

    public ProjectionRecord(string projectionName, string entityId, object state)
    {
        ProjectionName = projectionName;
        EntityId = entityId;
        State = state;
        UpdatedUtc = DateTime.UtcNow;
    }
}
```

---

# PROJECTION STATE STORE

Create:

storage/ProjectionStateStore.cs

Purpose:

Stores projection state.

Implementation:

```csharp
public sealed class ProjectionStateStore
{
    private readonly Dictionary<string, ProjectionRecord> _records = new();

    public void Save(ProjectionRecord record)
    {
        var key = record.ProjectionName + ":" + record.EntityId;

        _records[key] = record;
    }

    public ProjectionRecord? Get(string projectionName, string entityId)
    {
        var key = projectionName + ":" + entityId;

        if (_records.TryGetValue(key, out var record))
            return record;

        return null;
    }

    public IReadOnlyCollection<ProjectionRecord> GetAll()
    {
        return _records.Values.ToList().AsReadOnly();
    }
}
```

---

# PROJECTION REGISTRY

Create:

registry/ProjectionRegistry.cs

Purpose:

Registers projection handlers.

Implementation:

```csharp
public sealed class ProjectionRegistry
{
    private readonly Dictionary<string, string> _projections = new();

    public void Register(string eventType, string projectionName)
    {
        _projections[eventType] = projectionName;
    }

    public string Resolve(string eventType)
    {
        if (!_projections.TryGetValue(eventType, out var projection))
            throw new InvalidOperationException("Projection not registered");

        return projection;
    }

    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return _projections;
    }
}
```

---

# PROJECTION ENGINE

Create:

engine/ProjectionEngine.cs

Purpose:

Applies events to projections.

Implementation:

```csharp
public sealed class ProjectionEngine
{
    private readonly ProjectionRegistry _registry;
    private readonly ProjectionStateStore _store;

    public ProjectionEngine(
        ProjectionRegistry registry,
        ProjectionStateStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void Apply(string eventType, string entityId, object state)
    {
        var projection = _registry.Resolve(eventType);

        var record = new ProjectionRecord(projection, entityId, state);

        _store.Save(record);
    }
}
```

---

# PROJECTION WORKER

Create:

workers/ProjectionWorker.cs

Purpose:

Consumes events and applies projections.

Implementation:

```csharp
public sealed class ProjectionWorker
{
    private readonly ProjectionEngine _engine;

    public ProjectionWorker(ProjectionEngine engine)
    {
        _engine = engine;
    }

    public void Handle(string eventType, string entityId, object state)
    {
        _engine.Apply(eventType, entityId, state);
    }
}
```

---

# DEBUG ENDPOINT

Add endpoint:

/dev/runtime/projections

Returns:

• registered projections  
• stored records count  

Example response:

```json
{
  "projections": {
    "CapitalContributionRecorded": "SPVCapitalProjection",
    "RevenueRecorded": "RevenueProjection"
  },
  "records": 125
}
```

---

# UNIT TESTS

Create project:

tests/runtime/projections/

Add tests:

ProjectionStateStoreTests.cs

Verify:

• save projection record  
• retrieve record  
• retrieve all  

ProjectionRegistryTests.cs

Verify:

• register mapping  
• resolve mapping  

ProjectionEngineTests.cs

Verify:

• projection application  
• state store update  

ProjectionWorkerTests.cs

Verify:

• event handling delegation  

---

# BUILD SUCCESS CRITERIA

Build succeeds with:

0 errors  
0 warnings  

All tests pass.

Debug endpoint returns projection registry and record counts.

---

# PHASE RESULT

After this phase the runtime can:

• consume events from the Event Fabric  
• update read models  
• support CQRS read queries  

This prepares the runtime for:

PHASE 1.22 — RELIABILITY RUNTIME