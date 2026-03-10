# WHYCESPACE WBSM v3
# PHASE 1.5 — ENGINE INVOCATION SYSTEM

You are implementing **Phase 1.5 of the Whycespace system**.

This phase implements the **Engine Invocation System**.

The engine invocation system ensures that:

• workflows do not execute business logic  
• engines perform deterministic execution  
• runtime controls engine invocation  
• engines remain stateless  

Engines must NEVER call other engines.

All invocation must pass through the runtime.

---

# OBJECTIVES

1 Implement Engine Registry  
2 Implement Engine Resolver  
3 Implement Engine Invocation Manager  
4 Implement Workflow Step Engine Executor  
5 Integrate engine invocation with Workflow Runtime  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/engine/
```

Structure:

```
src/runtime/engine/

├── registry/
├── resolver/
├── invocation/
├── executor/
└── models/
```

Create project:

```
Whycespace.EngineRuntime.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.WorkflowRuntime
```

---

# ENGINE REGISTRY

Folder:

```
registry/
```

Create interface:

```
IEngineRegistry.cs
```

Purpose:

Register execution engines.

Example:

```csharp
public interface IEngineRegistry
{
    void Register(IEngine engine);

    IEngine? Resolve(string engineName);

    IReadOnlyCollection<string> ListEngines();
}
```

---

Create implementation:

```
EngineRegistry.cs
```

Use:

```
Dictionary<string, IEngine>
```

Methods:

```
Register
Resolve
ListEngines
```

Throw exception if engine not found.

---

# ENGINE RESOLVER

Folder:

```
resolver/
```

Create:

```
EngineResolver.cs
```

Purpose:

Resolve engine instances during workflow execution.

Example:

```csharp
public sealed class EngineResolver
{
    private readonly IEngineRegistry _registry;

    public EngineResolver(IEngineRegistry registry)
    {
        _registry = registry;
    }

    public IEngine Resolve(string engineName)
    {
        return _registry.Resolve(engineName)
            ?? throw new InvalidOperationException(
                $"Engine {engineName} not registered");
    }
}
```

---

# ENGINE INVOCATION MANAGER

Folder:

```
invocation/
```

Create:

```
EngineInvocationManager.cs
```

Responsibilities:

• create EngineContext  
• call engine.ExecuteAsync  
• capture EngineResult  
• return result to workflow executor  

Example:

```csharp
public sealed class EngineInvocationManager
{
    public async Task<EngineResult> InvokeAsync(
        IEngine engine,
        EngineContext context,
        CancellationToken cancellationToken
    )
    {
        return await engine.ExecuteAsync(
            context,
            cancellationToken
        );
    }
}
```

---

# WORKFLOW STEP ENGINE EXECUTOR

Folder:

```
executor/
```

Create:

```
WorkflowStepEngineExecutor.cs
```

Purpose:

Execute engines for workflow steps.

Flow:

```
1 Resolve engine
2 Build EngineContext
3 Invoke engine
4 Return EngineResult
```

Example:

```csharp
public sealed class WorkflowStepEngineExecutor
{
    private readonly EngineResolver _resolver;
    private readonly EngineInvocationManager _invocationManager;

    public WorkflowStepEngineExecutor(
        EngineResolver resolver,
        EngineInvocationManager invocationManager)
    {
        _resolver = resolver;
        _invocationManager = invocationManager;
    }

    public async Task<EngineResult> ExecuteAsync(
        WorkflowStep step,
        WorkflowInstance instance,
        CancellationToken cancellationToken)
    {
        var engine = _resolver.Resolve(step.EngineName);

        var context = new EngineContext(
            Guid.NewGuid(),
            instance.WorkflowName,
            step.StepName,
            instance.WorkflowInstanceId.ToString(),
            instance.Input
        );

        return await _invocationManager.InvokeAsync(
            engine,
            context,
            cancellationToken
        );
    }
}
```

---

# ENGINE REGISTRATION

Create bootstrap file:

```
EngineBootstrapper.cs
```

Purpose:

Register engines during system startup.

Example:

```
registry.Register(new RideRequestValidationEngine());
registry.Register(new DriverMatchingEngine());
registry.Register(new RideCreationEngine());
```

---

# SAMPLE ENGINE

Create sample engine for testing.

Location:

```
src/engines/T2E_Execution/mobility/
```

Create:

```
RideRequestValidationEngine.cs
```

Example:

```csharp
public sealed class RideRequestValidationEngine : IEngine
{
    public string Name => "RideRequestValidationEngine";

    public Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            new EngineResult(
                true,
                Array.Empty<IEvent>(),
                null
            )
        );
    }
}
```

---

# UPDATE WORKFLOW EXECUTOR

Modify WorkflowExecutor to use:

```
WorkflowStepEngineExecutor
```

Replace direct engine calls.

New flow:

```
WorkflowExecutor
 ↓
WorkflowStepEngineExecutor
 ↓
EngineResolver
 ↓
EngineInvocationManager
 ↓
Engine
```

---

# UNIT TESTS

Create project:

```
tests/engine-runtime/
```

Tests:

```
EngineRegistryTests.cs
EngineResolverTests.cs
EngineInvocationManagerTests.cs
WorkflowStepEngineExecutorTests.cs
```

Test cases:

```
register engine
resolve engine
invoke engine
execute workflow step
```

---

# DEBUG ENDPOINTS

Add debug endpoints.

GET

```
/dev/engines
```

Return:

```json
{
  "engines": [
    "RideRequestValidationEngine",
    "DriverMatchingEngine",
    "RideCreationEngine"
  ]
}
```

---

POST

```
/dev/engines/invoke
```

Accept:

```
EngineInvocationEnvelope
```

Return:

```
EngineResult
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
4 passed
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
4 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.5 is complete when:

• engines can be registered  
• engines can be resolved  
• workflow runtime invokes engines  
• execution results return correctly  
• tests pass  
• debug endpoints respond  

End of Phase 1.5.