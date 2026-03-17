namespace Whycespace.WSS.Workflows.Tests;

using Whycespace.Engines.T1M.WSS.Workflows;
using Whycespace.Systems.Midstream.WSS.Stores;

public class WorkflowStateStoreTests
{
    private readonly WorkflowStateStore _store;

    public WorkflowStateStoreTests()
    {
        _store = new WorkflowStateStore();
    }

    private WorkflowStateRecord CreateRecord(
        string instanceId = "wf-test-20260315-abc",
        string workflowId = "order-processing") =>
        new(
            instanceId,
            workflowId,
            "step-1",
            new List<WorkflowStepState>(),
            new List<WorkflowStepState>(),
            0,
            false,
            DateTimeOffset.UtcNow
        );

    [Fact]
    public void PersistWorkflowState_ShouldStoreState()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        var retrieved = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(retrieved);
        Assert.Equal("order-processing", retrieved.WorkflowId);
        Assert.Equal("step-1", retrieved.CurrentStep);
    }

    [Fact]
    public void PersistWorkflowState_ShouldUpsertExisting()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        var updated = record with { CurrentStep = "step-2" };
        _store.PersistWorkflowState(updated);

        var retrieved = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(retrieved);
        Assert.Equal("step-2", retrieved.CurrentStep);
    }

    [Fact]
    public void GetWorkflowState_MissingState_ShouldReturnNull()
    {
        var result = _store.GetWorkflowState("wf-nonexistent-20260315-xyz");
        Assert.Null(result);
    }

    [Fact]
    public void UpdateStepState_ShouldCreateNewStep()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Running);

        var state = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(state);
    }

    [Fact]
    public void UpdateStepState_ShouldTrackCompletion()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Running);
        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Completed);

        var completed = _store.ListCompletedSteps("wf-test-20260315-abc");
        Assert.Single(completed);
        Assert.Equal("step-1", completed[0].StepId);
        Assert.Equal(StepStatus.Completed, completed[0].Status);
        Assert.NotNull(completed[0].CompletedAt);
    }

    [Fact]
    public void UpdateStepState_MissingInstance_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _store.UpdateStepState("wf-missing-20260315-xyz", "step-1", StepStatus.Running));
    }

    [Fact]
    public void UpdateStepState_RetryingShouldIncrementRetryCount()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Running);
        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Failed);
        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Retrying);
        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Retrying);

        var state = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(state);
        Assert.Equal(2, state.RetryCount);
    }

    [Fact]
    public void UpdateStepState_TimedOutShouldSetTimeoutStatus()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Running);
        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.TimedOut);

        var state = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(state);
        Assert.True(state.TimeoutStatus);
        Assert.Single(state.FailedSteps);
        Assert.Equal(StepStatus.TimedOut, state.FailedSteps[0].Status);
    }

    [Fact]
    public void ListPendingSteps_ShouldReturnOnlyPending()
    {
        var pendingStep = new WorkflowStepState("step-pending", "step-pending", StepStatus.Pending, 0, null, null, null);
        var completedStep = new WorkflowStepState("step-done", "step-done", StepStatus.Completed, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        var record = CreateRecord() with
        {
            CompletedSteps = new List<WorkflowStepState> { completedStep }
        };

        _store.PersistWorkflowState(record);
        _store.UpdateStepState("wf-test-20260315-abc", "step-pending", StepStatus.Pending);

        var pending = _store.ListPendingSteps("wf-test-20260315-abc");
        Assert.Single(pending);
        Assert.Equal("step-pending", pending[0].StepId);
    }

    [Fact]
    public void ListPendingSteps_MissingInstance_ShouldReturnEmpty()
    {
        var result = _store.ListPendingSteps("wf-nonexistent-20260315-xyz");
        Assert.Empty(result);
    }

    [Fact]
    public void ListCompletedSteps_MissingInstance_ShouldReturnEmpty()
    {
        var result = _store.ListCompletedSteps("wf-nonexistent-20260315-xyz");
        Assert.Empty(result);
    }

    [Fact]
    public async Task ConcurrentUpdateStepState_ShouldNotThrow()
    {
        var record = CreateRecord();
        _store.PersistWorkflowState(record);

        _store.UpdateStepState("wf-test-20260315-abc", "step-1", StepStatus.Running);

        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() =>
            {
                var stepId = $"step-{i}";
                _store.UpdateStepState("wf-test-20260315-abc", stepId, StepStatus.Running);
                _store.UpdateStepState("wf-test-20260315-abc", stepId, StepStatus.Completed);
            })).ToArray();

        await Task.WhenAll(tasks);

        var state = _store.GetWorkflowState("wf-test-20260315-abc");
        Assert.NotNull(state);
    }

    [Fact]
    public void WorkflowRecovery_ShouldRestoreStepState()
    {
        var completedStep = new WorkflowStepState("step-1", "step-1", StepStatus.Completed, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);
        var failedStep = new WorkflowStepState("step-2", "step-2", StepStatus.Failed, 1, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow);

        var record = new WorkflowStateRecord(
            "wf-recovery-20260315-abc",
            "order-processing",
            "step-2",
            new List<WorkflowStepState> { completedStep },
            new List<WorkflowStepState> { failedStep },
            1,
            false,
            DateTimeOffset.UtcNow
        );

        _store.PersistWorkflowState(record);

        var recovered = _store.GetWorkflowState("wf-recovery-20260315-abc");
        Assert.NotNull(recovered);
        Assert.Single(recovered.CompletedSteps);
        Assert.Single(recovered.FailedSteps);
        Assert.Equal(1, recovered.RetryCount);

        _store.UpdateStepState("wf-recovery-20260315-abc", "step-2", StepStatus.Retrying);
        _store.UpdateStepState("wf-recovery-20260315-abc", "step-2", StepStatus.Running);
        _store.UpdateStepState("wf-recovery-20260315-abc", "step-2", StepStatus.Completed);

        var afterRecovery = _store.GetWorkflowState("wf-recovery-20260315-abc");
        Assert.NotNull(afterRecovery);
        Assert.Equal(2, afterRecovery.CompletedSteps.Count);
        Assert.Empty(afterRecovery.FailedSteps);
    }
}
