# WHYCESPACE WBSM v3
# PHASE 1.19 — ENGINE WORKER POOL RUNTIME

You are implementing **Phase 1.19 of the Whycespace system**.

This phase introduces the **Engine Worker Pool Runtime**, enabling parallel execution of engines inside partitions.

This improves:

• throughput  
• CPU utilization  
• horizontal scalability  

The worker pool sits **between the Partition Dispatcher and Engine Invocation Manager**.

---

# ARCHITECTURE RULES

Follow WBSM v3 strictly.

1️⃣ Engines NEVER call engines.

2️⃣ Runtime invokes engines.

3️⃣ Worker pools only schedule execution — they do not contain business logic.

4️⃣ Worker execution must be deterministic.

5️⃣ No external libraries.

6️⃣ All classes must be sealed.

---

# LOCATION

Create module:

src/runtime/worker-pool/

Structure:

src/runtime/worker-pool/

├── workers/  
├── pool/  
├── queue/  
├── scaling/  
└── models/

Create project:

Whycespace.WorkerPoolRuntime.csproj

Target framework:

net8.0

References:

Whycespace.Contracts  
Whycespace.EngineInvocation  

---

# OBJECTIVES

Implement:

1️⃣ EngineExecutionTask  
2️⃣ EngineExecutionQueue  
3️⃣ EngineWorker  
4️⃣ EngineWorkerPool  
5️⃣ EngineWorkerPoolManager  
6️⃣ WorkerScalingPolicy  

Add unit tests.

Add debug endpoints.

---

# ENGINE EXECUTION TASK

Create:

models/EngineExecutionTask.cs

Purpose:

Represents a queued engine execution.

Implementation:

```csharp
public sealed class EngineExecutionTask
{
    public string EngineName { get; }

    public object Input { get; }

    public EngineExecutionTask(string engineName, object input)
    {
        EngineName = engineName;
        Input = input;
    }
}
```

---

# ENGINE EXECUTION QUEUE

Create:

queue/EngineExecutionQueue.cs

Purpose:

Thread-safe queue for engine execution tasks.

Implementation:

```csharp
public sealed class EngineExecutionQueue
{
    private readonly Queue<EngineExecutionTask> _queue = new();

    private readonly object _lock = new();

    public void Enqueue(EngineExecutionTask task)
    {
        lock (_lock)
        {
            _queue.Enqueue(task);
        }
    }

    public EngineExecutionTask? Dequeue()
    {
        lock (_lock)
        {
            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _queue.Count;
        }
    }
}
```

---

# ENGINE WORKER

Create:

workers/EngineWorker.cs

Purpose:

Consumes execution tasks from the queue.

Implementation:

```csharp
public sealed class EngineWorker
{
    private readonly EngineExecutionQueue _queue;

    public EngineWorker(EngineExecutionQueue queue)
    {
        _queue = queue;
    }

    public EngineExecutionTask? FetchTask()
    {
        return _queue.Dequeue();
    }
}
```

Important:

Worker does NOT execute engines directly.

Actual execution remains in **EngineInvocationManager**.

---

# ENGINE WORKER POOL

Create:

pool/EngineWorkerPool.cs

Purpose:

Manages a pool of workers.

Implementation:

```csharp
public sealed class EngineWorkerPool
{
    private readonly List<EngineWorker> _workers;

    public EngineWorkerPool(int workerCount, EngineExecutionQueue queue)
    {
        _workers = new List<EngineWorker>();

        for (var i = 0; i < workerCount; i++)
        {
            _workers.Add(new EngineWorker(queue));
        }
    }

    public IReadOnlyCollection<EngineWorker> Workers()
    {
        return _workers.AsReadOnly();
    }
}
```

---

# WORKER POOL MANAGER

Create:

pool/EngineWorkerPoolManager.cs

Purpose:

Central runtime manager for worker pools.

Implementation:

```csharp
public sealed class EngineWorkerPoolManager
{
    private readonly EngineWorkerPool _pool;

    public EngineWorkerPoolManager(EngineWorkerPool pool)
    {
        _pool = pool;
    }

    public IReadOnlyCollection<EngineWorker> GetWorkers()
    {
        return _pool.Workers();
    }
}
```

---

# WORKER SCALING POLICY

Create:

scaling/WorkerScalingPolicy.cs

Purpose:

Defines scaling strategy for worker pools.

Implementation:

```csharp
public sealed class WorkerScalingPolicy
{
    public int MinimumWorkers { get; }

    public int MaximumWorkers { get; }

    public WorkerScalingPolicy(int minimumWorkers, int maximumWorkers)
    {
        MinimumWorkers = minimumWorkers;
        MaximumWorkers = maximumWorkers;
    }
}
```

---

# DEBUG ENDPOINT

Add endpoint:

/dev/runtime/worker-pool

Returns:

• worker count  
• queue size  

Example response:

```json
{
  "workers": 8,
  "queueSize": 42
}
```

---

# UNIT TESTS

Create project:

tests/runtime/worker-pool/

Add tests:

EngineExecutionQueueTests.cs

Verify:

• enqueue  
• dequeue  
• queue size  

EngineWorkerPoolTests.cs

Verify:

• worker pool size  
• worker retrieval  

WorkerScalingPolicyTests.cs

Verify:

• scaling limits  

---

# BUILD SUCCESS CRITERIA

Build succeeds with:

0 errors  
0 warnings  

All tests pass.

Debug endpoint returns worker pool metrics.

---

# PHASE RESULT

After this phase the runtime can:

• queue engine executions  
• schedule tasks to worker pools  
• run engines in parallel  

This prepares the runtime for:

PHASE 1.20 — EVENT FABRIC RUNTIME.