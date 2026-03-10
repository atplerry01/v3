# WHYCESPACE WBSM v3
# PHASE 1.3 — WORKFLOW RUNTIME CORE

You are implementing **Phase 1.3 of the Whycespace system**.

This phase implements the **Workflow Runtime Core**, responsible for executing workflow graphs defined by WSS.

The workflow runtime is responsible for:

• executing workflow graphs  
• invoking execution engines  
• managing workflow context  
• producing workflow events  

Workflows must contain **NO business logic**.

They only define:

```
Workflow Graph → Engine Execution Sequence
```

All logic belongs in **execution engines (T2E)**.

---

# OBJECTIVES

1 Implement Workflow Registry  
2 Implement Workflow Runtime  
3 Implement Workflow Executor  
4 Implement Workflow Step Execution  
5 Integrate Engine Invocation  
6 Create Workflow Instance Context  
7 Implement unit tests  
8 Add debug endpoints  

---

# LOCATION

Create workflow runtime inside:

```
src/runtime/workflow/
```

Structure:

```
src/runtime/workflow/

├── registry/
├── executor/
├── runtime/
├── context/
├── step/
└── events/
```

Create project:

```
Whycespace.WorkflowRuntime.csproj
```

Target framework:

```
net8.0
```

Reference project:

```
Whycespace.Contracts
```

---

# WORKFLOW REGISTRY

Folder:

```
registry/
```

Create:

```
IWorkflowRegistry.cs
```

Purpose:

Store workflow graphs.

Example:

```csharp
public interface IWorkflowRegistry
{
    void Register(WorkflowGraph workflow);

    WorkflowGraph? Resolve(string workflowName);
}
```

Create implementation:

```
WorkflowRegistry.cs
```

Use:

```
Dictionary<string, WorkflowGraph>
```

Methods:

```
Register
Resolve
```

---

# WORKFLOW CONTEXT

Folder:

```
context/
```

Create:

```
WorkflowInstance.cs
```

Fields:

```
WorkflowInstanceId
WorkflowName
PartitionKey
CurrentStepIndex
Input
```

Example:

```csharp
public sealed class WorkflowInstance
{
    public Guid WorkflowInstanceId { get; }

    public string WorkflowName { get; }

    public int CurrentStepIndex { get; set; }

    public object? Input { get; }

    public WorkflowInstance(Guid id, string workflowName, object? input)
    {
        WorkflowInstanceId = id;
        WorkflowName = workflowName;
        Input = input;
        CurrentStepIndex = 0;
    }
}
```

---

# WORKFLOW EXECUTOR

Folder:

```
executor/
```

Create:

```
IWorkflowExecutor.cs
```

```csharp
public interface IWorkflowExecutor
{
    Task<ExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken
    );
}
```

Create implementation:

```
WorkflowExecutor.cs
```

Responsibilities:

```
1 Load workflow graph
2 Create workflow instance
3 Execute steps sequentially
4 Invoke engines
5 Collect events
6 Return execution result
```

---

# STEP EXECUTION

Folder:

```
step/
```

Create:

```
WorkflowStepExecutor.cs
```

Purpose:

Execute a single workflow step.

Flow:

```
1 Get engine name
2 Build EngineContext
3 Invoke engine
4 Capture EngineResult
5 Return events
```

Example:

```csharp
public sealed class WorkflowStepExecutor
{
    public async Task<EngineResult> ExecuteStepAsync(
        WorkflowStep step,
        WorkflowInstance instance,
        IEngine engine
    )
    {
        // step execution
    }
}
```

---

# WORKFLOW RUNTIME

Folder:

```
runtime/
```

Create:

```
WorkflowRuntime.cs
```

Responsibilities:

```
receive workflow execution requests
call executor
return execution result
```

Example:

```csharp
public sealed class WorkflowRuntime
{
    private readonly IWorkflowExecutor _executor;

    public WorkflowRuntime(IWorkflowExecutor executor)
    {
        _executor = executor;
    }

    public Task<ExecutionResult> ExecuteAsync(
        WorkflowExecutionRequest request,
        CancellationToken cancellationToken
    )
    {
        return _executor.ExecuteAsync(request, cancellationToken);
    }
}
```

---

# WORKFLOW EVENTS

Folder:

```
events/
```

Create:

```
WorkflowStartedEvent.cs
WorkflowCompletedEvent.cs
```

Example:

```csharp
public sealed record WorkflowStartedEvent(
    Guid EventId,
    string WorkflowName,
    Guid WorkflowInstanceId
) : EventBase(
    EventId,
    "WorkflowStarted",
    WorkflowInstanceId,
    Timestamp.Now()
);
```

---

# SAMPLE WORKFLOW GRAPH (WSS)

Workflow definitions remain in WSS.

Location:

```
src/system/midstream/WSS/workflows/
```

Create example workflow:

```
TaxiRideRequestWorkflow.cs
```

Example:

```csharp
public static class TaxiRideRequestWorkflow
{
    public static WorkflowGraph Build()
    {
        return new WorkflowGraph(
            "TaxiRideRequestWorkflow",
            new[]
            {
                new WorkflowStep("ValidateRideRequest", "RideRequestValidationEngine"),
                new WorkflowStep("FindDriver", "DriverMatchingEngine"),
                new WorkflowStep("CreateRide", "RideCreationEngine")
            }
        );
    }
}
```

---

# ENGINE INVOCATION

During step execution:

EngineContext must contain:

```
InvocationId
WorkflowId
WorkflowStep
PartitionKey
Input
```

Invoke engine:

```
engine.ExecuteAsync(context)
```

---

# UNIT TESTS

Create project:

```
tests/workflow-runtime/
```

Tests:

```
WorkflowRegistryTests.cs
WorkflowExecutorTests.cs
WorkflowRuntimeTests.cs
```

Test cases:

```
workflow registration
workflow resolution
workflow step execution
workflow runtime execution
```

---

# DEBUG ENDPOINTS

Add debug endpoints in FoundationHost.

GET

```
/dev/workflows
```

Return registered workflows.

Example:

```json
{
  "workflows": [
    "TaxiRideRequestWorkflow"
  ]
}
```

POST

```
/dev/workflows/run
```

Accept:

```
WorkflowExecutionRequest
```

Return:

```
ExecutionResult
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

Phase 1.3 is complete when:

• workflow runtime compiles  
• workflow graphs execute  
• engines can be invoked  
• tests pass  
• debug endpoints respond  

End of Phase 1.3.