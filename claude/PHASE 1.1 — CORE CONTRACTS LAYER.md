# WHYCESPACE WBSM v3
# PHASE 1.1 — CORE CONTRACTS LAYER

You are implementing **Phase 1.1 of the Whycespace system**.

This phase introduces the **Core Contracts Layer** used by:

• Commands  
• Events  
• Workflows  
• Engines  
• Runtime execution  
• Event Fabric  
• Projections  

These contracts form the **fundamental protocol of the system**.

All other modules depend on these contracts.

Follow the **Claude Bootstrap Rules strictly**.

---

# OBJECTIVES

1 Create system-wide runtime contracts  
2 Define Command model  
3 Define Event model  
4 Define Engine invocation protocol  
5 Define Workflow contracts  
6 Define deterministic execution models  
7 Create unit tests  
8 Ensure solution builds successfully  

---

# LOCATION

Create contracts inside:

```
src/shared/contracts/
```

Structure:

```
src/shared/contracts/

├── commands/
├── events/
├── engines/
├── workflows/
├── runtime/
└── primitives/
```

Create project:

```
Whycespace.Contracts.csproj
```

Target framework:

```
net8.0
```

This project provides **shared contracts for the entire system**.

---

# CREATE PRIMITIVES

Folder:

```
primitives/
```

Create files:

```
GuidId.cs
Timestamp.cs
```

GuidId.cs

Purpose:

Immutable identifier wrapper.

Example:

```csharp
public readonly record struct GuidId(Guid Value)
{
    public static GuidId New() => new(Guid.NewGuid());
}
```

Timestamp.cs

```csharp
public readonly record struct Timestamp(DateTime Value)
{
    public static Timestamp Now() => new(DateTime.UtcNow);
}
```

---

# CREATE COMMAND CONTRACT

Folder:

```
commands/
```

Create:

```
ICommand.cs
```

```csharp
public interface ICommand
{
    Guid CommandId { get; }
    Timestamp Timestamp { get; }
}
```

Create base record:

```
CommandBase.cs
```

```csharp
public abstract record CommandBase(
    Guid CommandId,
    Timestamp Timestamp
) : ICommand;
```

---

# CREATE EVENT CONTRACT

Folder:

```
events/
```

Create:

```
IEvent.cs
```

```csharp
public interface IEvent
{
    Guid EventId { get; }
    string EventType { get; }
    Guid AggregateId { get; }
    Timestamp Timestamp { get; }
}
```

Create base record:

```
EventBase.cs
```

```csharp
public abstract record EventBase(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    Timestamp Timestamp
) : IEvent;
```

---

# CREATE ENGINE CONTRACT

Folder:

```
engines/
```

Create:

```
EngineContext.cs
```

Properties:

```
InvocationId
WorkflowId
WorkflowStep
PartitionKey
Input
```

Example:

```csharp
public sealed record EngineContext(
    Guid InvocationId,
    string WorkflowId,
    string WorkflowStep,
    string PartitionKey,
    object? Input
);
```

Create:

```
EngineResult.cs
```

```csharp
public sealed record EngineResult(
    bool Success,
    IReadOnlyCollection<IEvent> Events,
    object? Output
);
```

Create:

```
IEngine.cs
```

```csharp
public interface IEngine
{
    string Name { get; }

    Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken
    );
}
```

---

# ENGINE INVOCATION ENVELOPE

Create:

```
EngineInvocationEnvelope.cs
```

Purpose:

Transport structure used by runtime.

Fields:

```
InvocationId
EngineName
WorkflowId
WorkflowStep
PartitionKey
Payload
```

Example:

```csharp
public sealed record EngineInvocationEnvelope(
    Guid InvocationId,
    string EngineName,
    string WorkflowId,
    string WorkflowStep,
    string PartitionKey,
    object Payload
);
```

---

# CREATE WORKFLOW CONTRACT

Folder:

```
workflows/
```

Create:

```
WorkflowStep.cs
```

```csharp
public sealed record WorkflowStep(
    string StepName,
    string EngineName
);
```

Create:

```
WorkflowGraph.cs
```

```csharp
public sealed record WorkflowGraph(
    string WorkflowName,
    IReadOnlyList<WorkflowStep> Steps
);
```

Create:

```
WorkflowContext.cs
```

```csharp
public sealed record WorkflowContext(
    Guid WorkflowInstanceId,
    string WorkflowName,
    string PartitionKey,
    object? Input
);
```

---

# CREATE RUNTIME CONTRACTS

Folder:

```
runtime/
```

Create:

```
ExecutionResult.cs
```

```csharp
public sealed record ExecutionResult(
    bool Success,
    IReadOnlyCollection<IEvent> Events
);
```

Create:

```
WorkflowExecutionRequest.cs
```

```csharp
public sealed record WorkflowExecutionRequest(
    Guid WorkflowInstanceId,
    string WorkflowName,
    object Input
);
```

---

# ADD PROJECT TO SOLUTION

Add project:

```
Whycespace.Contracts
```

Reference this project from:

```
runtime
engines
system modules
platform
```

---

# UNIT TESTS

Create test folder:

```
tests/contracts/
```

Add project:

```
Whycespace.Contracts.Tests
```

Create tests:

```
CommandTests.cs
EventTests.cs
EngineContextTests.cs
WorkflowGraphTests.cs
```

Test cases:

• Command creation  
• Event creation  
• WorkflowGraph initialization  
• EngineContext structure  

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

# DEBUG API

Add debug endpoint in FoundationHost.

```
GET /dev/contracts
```

Response:

```json
{
  "contracts": [
    "ICommand",
    "IEvent",
    "IEngine",
    "WorkflowGraph",
    "WorkflowContext"
  ]
}
```

---

# OUTPUT FORMAT

Return:

1 Files Created  
2 Repository Tree  
3 Build Result  
4 Tests Result  
5 Debug Endpoints  

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

Phase 1.1 is complete when:

• contracts project exists  
• contracts compile  
• unit tests pass  
• debug endpoint returns contracts  
• solution builds successfully  

End of Phase 1.1.