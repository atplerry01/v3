# WHYCESPACE WBSM v3
# PHASE 1.6 — PARTITION EXECUTION MODEL

You are implementing **Phase 1.6 of the Whycespace system**.

This phase introduces the **Partition Execution Model**, which enables horizontal scalability for workflow execution.

Workflows must be routed deterministically to execution partitions.

Each workflow instance must always execute on the same partition.

This guarantees:

• ordered workflow execution  
• deterministic event processing  
• distributed scalability  

---

# OBJECTIVES

1 Implement Partition Key Resolver  
2 Implement Partition Router  
3 Implement Workflow Partition Dispatcher  
4 Implement Partition Worker  
5 Integrate partitions with Runtime Dispatcher  
6 Implement unit tests  
7 Add debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/partition/
```

Structure:

```
src/runtime/partition/

├── resolver/
├── router/
├── dispatcher/
├── worker/
└── models/
```

Create project:

```
Whycespace.PartitionRuntime.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.RuntimeDispatcher
```

---

# PARTITION KEY RESOLVER

Folder:

```
resolver/
```

Create:

```
IPartitionKeyResolver.cs
```

Purpose:

Determine partition key for a workflow execution.

Example:

```csharp
public interface IPartitionKeyResolver
{
    string ResolvePartitionKey(CommandEnvelope command);
}
```

---

Create implementation:

```
PartitionKeyResolver.cs
```

Logic:

Use aggregate identifiers when available.

Examples:

Ride workflows → RiderId  
Property workflows → PropertyId  
Economic workflows → SPVId  

Fallback:

CommandId

---

# PARTITION ROUTER

Folder:

```
router/
```

Create:

```
IPartitionRouter.cs
```

```csharp
public interface IPartitionRouter
{
    int ResolvePartition(string partitionKey);
}
```

---

Create implementation:

```
PartitionRouter.cs
```

Use consistent hashing.

Example:

```text
partition = Hash(partitionKey) % partitionCount
```

Where:

```
partitionCount = configurable
```

Example default:

```
16 partitions
```

---

# PARTITION MODEL

Folder:

```
models/
```

Create:

```
PartitionAssignment.cs
```

Example:

```csharp
public sealed record PartitionAssignment(
    string PartitionKey,
    int PartitionId
);
```

---

# WORKFLOW PARTITION DISPATCHER

Folder:

```
dispatcher/
```

Create:

```
WorkflowPartitionDispatcher.cs
```

Purpose:

Route workflow execution requests to correct partition.

Flow:

```
1 Resolve partition key
2 Resolve partition id
3 Send execution request to partition worker
```

Example:

```csharp
public sealed class WorkflowPartitionDispatcher
{
    public Task DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken
    )
}
```

---

# PARTITION WORKER

Folder:

```
worker/
```

Create:

```
PartitionWorker.cs
```

Purpose:

Process workflow executions for a partition.

Responsibilities:

• maintain execution queue  
• process workflow requests sequentially  
• invoke workflow runtime  

Example:

```csharp
public sealed class PartitionWorker
{
    private readonly int _partitionId;

    public Task EnqueueAsync(
        WorkflowExecutionRequest request
    )
}
```

---

# PARTITION WORKER POOL

Create:

```
PartitionWorkerPool.cs
```

Purpose:

Manage workers for all partitions.

Example:

```csharp
public sealed class PartitionWorkerPool
{
    private readonly Dictionary<int, PartitionWorker> _workers;

    public PartitionWorkerPool(int partitionCount)
    {
        _workers = new Dictionary<int, PartitionWorker>();

        for (int i = 0; i < partitionCount; i++)
        {
            _workers[i] = new PartitionWorker(i);
        }
    }

    public PartitionWorker GetWorker(int partitionId)
    {
        return _workers[partitionId];
    }
}
```

---

# UPDATE RUNTIME DISPATCHER

Modify RuntimeDispatcher.

Instead of executing workflows directly.

New flow:

```
CommandDispatcher
 ↓
RuntimeDispatcher
 ↓
PartitionKeyResolver
 ↓
PartitionRouter
 ↓
WorkflowPartitionDispatcher
 ↓
PartitionWorker
 ↓
WorkflowRuntime
```

---

# SAMPLE EXECUTION FLOW

Example:

```
RequestRideCommand
 ↓
PartitionKeyResolver → RiderId
 ↓
PartitionRouter → Partition 3
 ↓
PartitionWorker(3)
 ↓
WorkflowRuntime.Execute()
```

This ensures **all rider workflows run on the same partition**.

---

# UNIT TESTS

Create project:

```
tests/partition-runtime/
```

Tests:

```
PartitionKeyResolverTests.cs
PartitionRouterTests.cs
PartitionDispatcherTests.cs
PartitionWorkerTests.cs
```

Test cases:

```
resolve partition key
resolve partition id
dispatch workflow to correct partition
partition worker execution
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/partitions
```

Return:

```json
{
  "partitionCount": 16
}
```

---

GET

```
/dev/partitions/workers
```

Return active workers.

Example:

```json
{
  "workers": [
    { "partitionId": 0 },
    { "partitionId": 1 }
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

Phase 1.6 is complete when:

• partition resolver works  
• partition router works  
• workflows route to correct partition  
• partition workers process workflows  
• tests pass  
• debug endpoints respond  

End of Phase 1.6.