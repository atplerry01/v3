# WHYCESPACE WBSM v3
# PHASE 1.7 — ENGINE WORKER POOLS

You are implementing **Phase 1.7 of the Whycespace system**.

This phase introduces **Engine Worker Pools**, which enable concurrent engine execution while maintaining deterministic workflow order inside partitions.

Key principles:

• Workflow partitions remain ordered  
• Engine execution may run concurrently  
• Worker pools increase throughput  
• Engine workers must remain stateless  

Workers process **EngineInvocationEnvelope** messages generated during workflow execution.

---

# OBJECTIVES

1 Implement Engine Worker  
2 Implement Engine Worker Pool  
3 Implement Engine Execution Queue  
4 Implement Worker Supervisor  
5 Integrate workers with WorkflowStepEngineExecutor  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/engine-workers/
```

Structure:

```
src/runtime/engine-workers/

├── worker/
├── pool/
├── queue/
├── supervisor/
└── models/
```

Create project:

```
Whycespace.EngineWorkerRuntime.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.EngineRuntime
```

---

# ENGINE EXECUTION QUEUE

Folder:

```
queue/
```

Create:

```
EngineExecutionQueue.cs
```

Purpose:

Queue engine invocation requests before workers process them.

Example:

```csharp
public sealed class EngineExecutionQueue
{
    private readonly ConcurrentQueue<EngineInvocationEnvelope> _queue;

    public EngineExecutionQueue()
    {
        _queue = new ConcurrentQueue<EngineInvocationEnvelope>();
    }

    public void Enqueue(EngineInvocationEnvelope invocation)
    {
        _queue.Enqueue(invocation);
    }

    public bool TryDequeue(out EngineInvocationEnvelope invocation)
    {
        return _queue.TryDequeue(out invocation);
    }
}
```

---

# ENGINE WORKER

Folder:

```
worker/
```

Create:

```
EngineWorker.cs
```

Responsibilities:

• pull invocation from queue  
• resolve engine  
• execute engine  
• return EngineResult  

Example:

```csharp
public sealed class EngineWorker
{
    private readonly EngineResolver _resolver;
    private readonly EngineExecutionQueue _queue;

    public EngineWorker(
        EngineResolver resolver,
        EngineExecutionQueue queue)
    {
        _resolver = resolver;
        _queue = queue;
    }

    public async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var invocation))
            {
                var engine = _resolver.Resolve(invocation.EngineName);

                var context = new EngineContext(
                    invocation.InvocationId,
                    invocation.WorkflowId,
                    invocation.WorkflowStep,
                    invocation.PartitionKey,
                    invocation.Payload
                );

                await engine.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
```

---

# ENGINE WORKER POOL

Folder:

```
pool/
```

Create:

```
EngineWorkerPool.cs
```

Purpose:

Manage multiple workers.

Example:

```csharp
public sealed class EngineWorkerPool
{
    private readonly List<EngineWorker> _workers;

    public EngineWorkerPool(
        int workerCount,
        EngineResolver resolver,
        EngineExecutionQueue queue)
    {
        _workers = new List<EngineWorker>();

        for (int i = 0; i < workerCount; i++)
        {
            _workers.Add(new EngineWorker(resolver, queue));
        }
    }

    public IReadOnlyCollection<EngineWorker> Workers => _workers;
}
```

---

# WORKER SUPERVISOR

Folder:

```
supervisor/
```

Create:

```
WorkerSupervisor.cs
```

Purpose:

Start and monitor worker threads.

Example:

```csharp
public sealed class WorkerSupervisor
{
    private readonly EngineWorkerPool _pool;

    public WorkerSupervisor(EngineWorkerPool pool)
    {
        _pool = pool;
    }

    public void Start(CancellationToken cancellationToken)
    {
        foreach (var worker in _pool.Workers)
        {
            Task.Run(() => worker.ProcessAsync(cancellationToken));
        }
    }
}
```

---

# INTEGRATE WITH ENGINE INVOCATION

Modify:

```
WorkflowStepEngineExecutor
```

Instead of calling engine directly.

New flow:

```
WorkflowExecutor
 ↓
WorkflowStepEngineExecutor
 ↓
EngineInvocationEnvelope created
 ↓
EngineExecutionQueue.Enqueue()
 ↓
EngineWorker processes invocation
```

---

# SAMPLE EXECUTION FLOW

Example workflow execution:

```
TaxiRideRequestWorkflow
```

Step 1:

```
RideRequestValidationEngine
```

Step 2:

```
DriverMatchingEngine
```

Step 3:

```
RideCreationEngine
```

Each step generates:

```
EngineInvocationEnvelope
```

Worker pool executes engines concurrently.

---

# UNIT TESTS

Create project:

```
tests/engine-worker-runtime/
```

Tests:

```
EngineExecutionQueueTests.cs
EngineWorkerTests.cs
EngineWorkerPoolTests.cs
WorkerSupervisorTests.cs
```

Test cases:

```
queue enqueue/dequeue
worker execution
worker pool initialization
supervisor startup
```

---

# DEBUG ENDPOINTS

Add debug endpoints.

GET

```
/dev/engine-workers
```

Return:

```json
{
  "workerCount": 4
}
```

---

GET

```
/dev/engine-workers/status
```

Return worker status.

Example:

```json
{
  "workers": [
    { "workerId": 1, "status": "running" },
    { "workerId": 2, "status": "running" }
  ]
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

Phase 1.7 is complete when:

• engine workers process invocation queue  
• worker pools start correctly  
• workers execute engines  
• tests pass  
• debug endpoints respond  

End of Phase 1.7.