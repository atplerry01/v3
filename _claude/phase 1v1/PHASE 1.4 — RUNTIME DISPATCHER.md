# WHYCESPACE WBSM v3
# PHASE 1.4 — RUNTIME DISPATCHER

You are implementing **Phase 1.4 of the Whycespace system**.

This phase implements the **Runtime Dispatcher**.

The dispatcher connects:

Command System → Workflow Runtime

It receives commands and resolves the workflow that should execute.

Dispatcher responsibilities:

• accept CommandEnvelope  
• resolve workflow  
• create WorkflowExecutionRequest  
• call WorkflowRuntime  
• return ExecutionResult  

Dispatcher must remain **stateless**.

---

# OBJECTIVES

1 Implement Runtime Dispatcher  
2 Implement Workflow Resolver  
3 Connect CommandDispatcher → WorkflowRuntime  
4 Create execution pipeline  
5 Implement unit tests  
6 Provide debug endpoints  

---

# LOCATION

Create dispatcher module inside:

```
src/runtime/dispatcher/
```

Structure:

```
src/runtime/dispatcher/

├── dispatcher/
├── resolver/
├── pipeline/
└── models/
```

Create project:

```
Whycespace.RuntimeDispatcher.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.CommandSystem
Whycespace.WorkflowRuntime
```

---

# WORKFLOW RESOLVER

Folder:

```
resolver/
```

Create interface:

```
IWorkflowResolver.cs
```

Purpose:

Resolve workflow name for a command.

Example:

```csharp
public interface IWorkflowResolver
{
    string ResolveWorkflow(string commandType);
}
```

---

Create implementation:

```
WorkflowResolver.cs
```

Use in-memory dictionary.

Example mapping:

```
RequestRideCommand → TaxiRideRequestWorkflow
CreatePropertyListingCommand → PropertyListingWorkflow
```

Methods:

```
ResolveWorkflow
```

Throw exception if mapping not found.

---

# RUNTIME DISPATCHER

Folder:

```
dispatcher/
```

Create:

```
IRuntimeDispatcher.cs
```

```csharp
public interface IRuntimeDispatcher
{
    Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken
    );
}
```

---

Create implementation:

```
RuntimeDispatcher.cs
```

Dependencies:

```
ICommandValidator
IIdempotencyRegistry
IWorkflowResolver
WorkflowRuntime
```

Execution flow:

```
1 Validate command
2 Check idempotency
3 Resolve workflow
4 Create WorkflowExecutionRequest
5 Call WorkflowRuntime
6 Return ExecutionResult
```

Example method:

```csharp
public async Task<ExecutionResult> DispatchAsync(
    CommandEnvelope command,
    CancellationToken cancellationToken
)
```

---

# EXECUTION PIPELINE

Folder:

```
pipeline/
```

Create:

```
ExecutionPipeline.cs
```

Purpose:

Encapsulate execution flow.

Steps:

```
ValidateCommand
CheckIdempotency
ResolveWorkflow
ExecuteWorkflow
```

Return:

```
ExecutionResult
```

---

# DISPATCHER MODEL

Folder:

```
models/
```

Create:

```
DispatchResult.cs
```

Properties:

```
Success
WorkflowName
Events
```

Example:

```csharp
public sealed record DispatchResult(
    bool Success,
    string WorkflowName,
    IReadOnlyCollection<IEvent> Events
);
```

---

# INTEGRATION WITH COMMAND SYSTEM

Modify CommandDispatcher.

Instead of returning `WorkflowExecutionRequest` directly.

CommandDispatcher must call:

```
RuntimeDispatcher.DispatchAsync()
```

Flow becomes:

```
CommandController
 ↓
CommandDispatcher
 ↓
RuntimeDispatcher
 ↓
WorkflowRuntime
```

---

# SAMPLE EXECUTION

Example flow:

```
RequestRideCommand
 ↓
CommandDispatcher
 ↓
RuntimeDispatcher
 ↓
TaxiRideRequestWorkflow
 ↓
WorkflowExecutor
 ↓
RideRequestValidationEngine
 ↓
DriverMatchingEngine
 ↓
RideCreationEngine
```

---

# UNIT TESTS

Create project:

```
tests/runtime-dispatcher/
```

Tests:

```
WorkflowResolverTests.cs
RuntimeDispatcherTests.cs
ExecutionPipelineTests.cs
```

Test cases:

```
resolve workflow correctly
dispatcher executes workflow
idempotency enforced
execution pipeline success
```

---

# DEBUG ENDPOINTS

Add debug endpoints in FoundationHost.

GET

```
/dev/runtime/dispatcher
```

Return:

```json
{
  "dispatcher": "active"
}
```

---

POST

```
/dev/runtime/dispatch
```

Accept:

```
CommandEnvelope
```

Return:

```
DispatchResult
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
4 Tests Result
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

Phase 1.4 is complete when:

• runtime dispatcher compiles  
• commands trigger workflows  
• workflows execute successfully  
• tests pass  
• debug endpoints work  

End of Phase 1.4.