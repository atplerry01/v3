# WHYCESPACE WBSM v3
# PHASE 1.9 — RELIABILITY ARCHITECTURE

You are implementing **Phase 1.9 of the Whycespace system**.

This phase introduces the **Reliability Architecture**, which ensures the runtime remains fault-tolerant and consistent in distributed execution environments.

The system must support:

• workflow state persistence  
• retry handling  
• idempotent execution  
• dead-letter routing  
• saga coordination  

All components must remain **deterministic**.

---

# OBJECTIVES

1 Implement Workflow State Store  
2 Implement Retry Engine  
3 Implement Dead Letter Queue  
4 Implement Saga Coordinator  
5 Integrate reliability with workflow runtime  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

src/runtime/reliability/

Structure:

src/runtime/reliability/

├── state/  
├── retry/  
├── dlq/  
├── saga/  
└── models/  

Create project:

Whycespace.Reliability.csproj

Target framework:

net10.0

References:

Whycespace.Contracts  
Whycespace.WorkflowRuntime  
Whycespace.RuntimeDispatcher  
Whycespace.EventFabric  

---

# WORKFLOW STATE STORE

Folder:

state/

Create interface:

IWorkflowStateStore.cs

Purpose:

Persist workflow execution state.

Example:

public interface IWorkflowStateStore
{
    Task SaveAsync(WorkflowInstance instance);

    Task<WorkflowInstance?> LoadAsync(Guid workflowInstanceId);

    Task<IReadOnlyCollection<WorkflowInstance>> GetActiveWorkflowsAsync();
}

---

Create implementation:

PostgresWorkflowStateStore.cs

Responsibilities:

• persist workflow state  
• load workflow state  
• maintain workflow progress  

Workflow state fields:

WorkflowInstanceId  
WorkflowName  
CurrentStepIndex  
PartitionKey  
WorkflowContext  
LastUpdated  

Persistence storage: PostgreSQL table.

---

# RETRY ENGINE

Folder:

retry/

Create:

RetryPolicyEngine.cs

Purpose:

Retry failed workflow steps.

Retry rules:

MaxRetries = configurable  
RetryDelay = configurable  

Example:

public sealed class RetryPolicyEngine
{
    private readonly int _maxRetries;

    public RetryPolicyEngine(int maxRetries)
    {
        _maxRetries = maxRetries;
    }

    public bool ShouldRetry(int attemptCount)
    {
        return attemptCount < _maxRetries;
    }
}

---

# DEAD LETTER QUEUE

Folder:

dlq/

Create:

DeadLetterPublisher.cs

Purpose:

Publish failed workflow executions to DLQ topic.

Kafka topic:

whyce.dlq.events

Example:

public sealed class DeadLetterPublisher
{
    private readonly IEventPublisher _publisher;

    public DeadLetterPublisher(IEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishAsync(
        EventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        return _publisher.PublishAsync(
            "whyce.dlq.events",
            envelope,
            cancellationToken
        );
    }
}

---

# SAGA COORDINATOR

Folder:

saga/

Create:

SagaCoordinator.cs

Purpose:

Coordinate long-running distributed workflows.

Responsibilities:

track distributed operations  
manage compensating actions  
ensure eventual consistency  

Example:

public sealed class SagaCoordinator
{
    public Task StartSagaAsync(Guid sagaId);

    public Task CompleteSagaAsync(Guid sagaId);

    public Task CompensateAsync(Guid sagaId);
}

---

# INTEGRATE WITH WORKFLOW EXECUTOR

Modify:

WorkflowExecutor

New flow:

WorkflowExecution  
 ↓  
Save workflow state  
 ↓  
Execute workflow step  
 ↓  
If failure  
 ↓  
RetryPolicyEngine.ShouldRetry()  
 ↓  
Retry step  

If retries exhausted:

DeadLetterPublisher.PublishAsync()

---

# WORKFLOW RECOVERY

On runtime startup:

Reload active workflows.

Example:

WorkflowStateStore.GetActiveWorkflowsAsync()

Resume from:

CurrentStepIndex

This ensures workflow recovery after crashes.

---

# SAGA EXAMPLE

Example workflow:

PropertyLettingWorkflow

Steps:

TenantApplication  
BackgroundCheck  
LeaseCreation  

If LeaseCreation fails:

SagaCoordinator triggers compensation:

CancelApplication  
ReleaseProperty  

---

# UNIT TESTS

Create project:

tests/reliability/

Tests:

WorkflowStateStoreTests.cs  
RetryPolicyEngineTests.cs  
DeadLetterPublisherTests.cs  
SagaCoordinatorTests.cs  

Test cases:

persist workflow state  
retry logic  
dead letter publishing  
saga lifecycle  

---

# DEBUG ENDPOINTS

Add endpoints:

GET

/dev/reliability/workflows

Return active workflow states.

Example:

{
  "activeWorkflows": 12
}

---

GET

/dev/reliability/retries

Return retry statistics.

Example:

{
  "retries": 3
}

---

GET

/dev/reliability/dlq

Return DLQ message count.

Example:

{
  "deadLetters": 2
}

---

# BUILD VALIDATION

Run:

dotnet build

Expected:

Build succeeded  
0 warnings  
0 errors  

---

# TEST VALIDATION

Run:

dotnet test

Expected:

Tests:  
4 passed  
0 failed  

---

# OUTPUT FORMAT

Return:

1 Files Created  
2 Repository Tree  
3 Build Result  
4 Test Result  
5 Debug Endpoints  

Example:

Build succeeded  
0 warnings  
0 errors  

Tests:  
4 passed  
0 failed  

---

# PHASE COMPLETION CRITERIA

Phase 1.9 is complete when:

• workflow state persists correctly  
• retries trigger correctly  
• failed messages go to DLQ  
• sagas coordinate distributed workflows  
• workflows recover after restart  
• tests pass  
• debug endpoints respond  

End of Phase 1.9.