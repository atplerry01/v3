# WHYCESPACE WBSM v3

# PHASE 2.0.9 — WHYCEID IDENTITY GRAPH ENGINE
(System + Engine Architecture)

You are implementing the **Identity Graph subsystem of WhyceID**.

The Identity Graph models **relationships between identities**.

This graph allows the system to represent:

• ownership relationships  
• delegation of authority  
• organizational hierarchy  
• governance relationships  
• service relationships  

Examples:

```
Identity A → owns → SPV B
Identity A → delegates → Identity B
Organization → employs → Identity
Guardian → supervises → Operator
```

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• IdentityRelation  
• IdentityGraph  

ENGINE COMPONENT

• AddIdentityRelationEngine  
• RemoveIdentityRelationEngine  
• IdentityRelationQueryEngine  

Also implement:

• Commands  
• Events  
• Unit tests  

---

# SYSTEM MODULE LOCATION

Extend:

```
src/system/upstream/WhyceID/
```

Create folder:

```
graph/
```

Structure:

```
graph/

├── IdentityRelation.cs
└── IdentityGraph.cs
```

---

# RELATION TYPE ENUM

Create:

```
graph/RelationType.cs
```

```csharp
public enum RelationType
{
    Owns = 1,
    Employs = 2,
    DelegatesAuthority = 3,
    Supervises = 4,
    MemberOf = 5
}
```

---

# IDENTITY RELATION MODEL

Create:

```
graph/IdentityRelation.cs
```

```csharp
public sealed class IdentityRelation
{
    public Guid FromIdentity { get; }

    public Guid ToIdentity { get; }

    public RelationType Type { get; }

    public DateTime CreatedAt { get; }

    public IdentityRelation(
        Guid fromIdentity,
        Guid toIdentity,
        RelationType type)
    {
        FromIdentity = fromIdentity;
        ToIdentity = toIdentity;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}
```

---

# IDENTITY GRAPH REGISTRY

Create:

```
graph/IdentityGraph.cs
```

```csharp
public sealed class IdentityGraph
{
    private readonly List<IdentityRelation> _relations = new();

    public void AddRelation(IdentityRelation relation)
    {
        _relations.Add(relation);
    }

    public void RemoveRelation(Guid from, Guid to, RelationType type)
    {
        _relations.RemoveAll(r =>
            r.FromIdentity == from &&
            r.ToIdentity == to &&
            r.Type == type);
    }

    public IReadOnlyList<IdentityRelation> GetRelations(Guid identity)
    {
        return _relations
            .Where(r => r.FromIdentity == identity || r.ToIdentity == identity)
            .ToList()
            .AsReadOnly();
    }
}
```

---

# COMMANDS

Create:

```
commands/AddIdentityRelationCommand.cs
```

```csharp
public sealed record AddIdentityRelationCommand(
    Guid FromIdentity,
    Guid ToIdentity,
    RelationType Type);
```

---

Create:

```
commands/RemoveIdentityRelationCommand.cs
```

```csharp
public sealed record RemoveIdentityRelationCommand(
    Guid FromIdentity,
    Guid ToIdentity,
    RelationType Type);
```

---

Create:

```
commands/QueryIdentityRelationsCommand.cs
```

```csharp
public sealed record QueryIdentityRelationsCommand(
    Guid IdentityId);
```

---

# EVENTS

Create:

```
events/IdentityRelationAddedEvent.cs
```

```csharp
public sealed record IdentityRelationAddedEvent(
    Guid FromIdentity,
    Guid ToIdentity,
    RelationType Type,
    DateTime CreatedAt);
```

---

Create:

```
events/IdentityRelationRemovedEvent.cs
```

```csharp
public sealed record IdentityRelationRemovedEvent(
    Guid FromIdentity,
    Guid ToIdentity,
    RelationType Type,
    DateTime RemovedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# ADD RELATION ENGINE

Create:

```
AddIdentityRelationEngine.cs
```

```csharp
public sealed class AddIdentityRelationEngine
{
    public IdentityRelationAddedEvent Execute(
        AddIdentityRelationCommand command,
        IdentityGraph graph)
    {
        var relation = new IdentityRelation(
            command.FromIdentity,
            command.ToIdentity,
            command.Type);

        graph.AddRelation(relation);

        return new IdentityRelationAddedEvent(
            command.FromIdentity,
            command.ToIdentity,
            command.Type,
            relation.CreatedAt);
    }
}
```

---

# REMOVE RELATION ENGINE

Create:

```
RemoveIdentityRelationEngine.cs
```

```csharp
public sealed class RemoveIdentityRelationEngine
{
    public IdentityRelationRemovedEvent Execute(
        RemoveIdentityRelationCommand command,
        IdentityGraph graph)
    {
        graph.RemoveRelation(
            command.FromIdentity,
            command.ToIdentity,
            command.Type);

        return new IdentityRelationRemovedEvent(
            command.FromIdentity,
            command.ToIdentity,
            command.Type,
            DateTime.UtcNow);
    }
}
```

---

# RELATION QUERY ENGINE

Create:

```
IdentityRelationQueryEngine.cs
```

```csharp
public sealed class IdentityRelationQueryEngine
{
    public IReadOnlyList<IdentityRelation> Execute(
        QueryIdentityRelationsCommand command,
        IdentityGraph graph)
    {
        return graph.GetRelations(command.IdentityId);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.IdentityGraph.Tests/
```

Tests:

```
IdentityRelationAddTests
IdentityRelationRemoveTests
IdentityGraphQueryTests
```

Example:

```csharp
[Fact]
public void IdentityRelation_ShouldBeAdded()
{
    var graph = new IdentityGraph();

    var engine = new AddIdentityRelationEngine();

    var result = engine.Execute(
        new AddIdentityRelationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RelationType.Owns),
        graph);

    Assert.NotNull(result);
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
0 errors
0 warnings
```

---

# SUCCESS CRITERIA

Relations can be added  
Relations can be removed  
Relations can be queried  
Identity graph tracks relationships correctly  
Unit tests pass  

---

# END OF PHASE 2.0.9