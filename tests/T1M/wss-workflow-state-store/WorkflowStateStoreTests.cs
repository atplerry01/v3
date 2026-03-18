using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;
using WorkflowInstanceStatus = Whycespace.Systems.Midstream.WSS.Execution.WorkflowInstanceStatus;

namespace Whycespace.WSS.WorkflowStateStore.Tests;

public class WorkflowStateStoreTests
{
    private readonly WssWorkflowStateStore _store;

    public WorkflowStateStoreTests()
    {
        _store = new WssWorkflowStateStore();
    }

    private WorkflowState CreateState(string instanceId = "wf-test-20260312-abc", string workflowId = "taxi-request") =>
        new(
            instanceId,
            workflowId,
            "1.0.0",
            string.Empty,
            new List<string>(),
            WorkflowInstanceStatus.Created,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object>()
        );

    // 1. Save workflow state
    [Fact]
    public void SaveState_ShouldPersistState()
    {
        var state = CreateState();

        _store.SaveState(state);

        var retrieved = _store.GetState("wf-test-20260312-abc");
        Assert.Equal("taxi-request", retrieved.WorkflowId);
        Assert.Equal(WorkflowInstanceStatus.Created, retrieved.Status);
    }

    // 2. Retrieve workflow state
    [Fact]
    public void GetState_ExistingState_ShouldReturnState()
    {
        var state = CreateState();
        _store.SaveState(state);

        var result = _store.GetState(state.InstanceId);

        Assert.Equal(state.InstanceId, result.InstanceId);
        Assert.Equal("1.0.0", result.WorkflowVersion);
    }

    // 3. Update workflow state
    [Fact]
    public void UpdateState_ShouldUpdateStepAndStatus()
    {
        var state = CreateState();
        _store.SaveState(state);

        var updated = _store.UpdateState(state.InstanceId, "find-driver", WorkflowInstanceStatus.Running);

        Assert.Equal("find-driver", updated.CurrentStep);
        Assert.Equal(WorkflowInstanceStatus.Running, updated.Status);
        Assert.True(updated.UpdatedAt >= state.UpdatedAt);
    }

    // 4. Track completed steps
    [Fact]
    public void AddCompletedStep_ShouldTrackSteps()
    {
        var state = CreateState();
        _store.SaveState(state);

        _store.AddCompletedStep(state.InstanceId, "validate-passenger");
        var updated = _store.AddCompletedStep(state.InstanceId, "find-driver");

        Assert.Equal(2, updated.CompletedSteps.Count);
        Assert.Contains("validate-passenger", updated.CompletedSteps);
        Assert.Contains("find-driver", updated.CompletedSteps);
    }

    [Fact]
    public void AddCompletedStep_DuplicateStep_ShouldNotAddTwice()
    {
        var state = CreateState();
        _store.SaveState(state);

        _store.AddCompletedStep(state.InstanceId, "validate-passenger");
        var updated = _store.AddCompletedStep(state.InstanceId, "validate-passenger");

        Assert.Single(updated.CompletedSteps);
    }

    // 5. Delete workflow state
    [Fact]
    public void DeleteState_ShouldRemoveState()
    {
        var state = CreateState();
        _store.SaveState(state);

        _store.DeleteState(state.InstanceId);

        Assert.Throws<KeyNotFoundException>(() => _store.GetState(state.InstanceId));
    }

    // 6. Concurrent state updates
    [Fact]
    public async Task UpdateState_ConcurrentUpdates_ShouldNotThrow()
    {
        var state = CreateState();
        _store.SaveState(state);

        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() => _store.UpdateState(state.InstanceId, $"step-{i}", WorkflowInstanceStatus.Running))).ToArray();

        await Task.WhenAll(tasks);

        var result = _store.GetState(state.InstanceId);
        Assert.Equal(WorkflowInstanceStatus.Running, result.Status);
    }

    // 7. Retrieve missing state
    [Fact]
    public void GetState_MissingState_ShouldThrow()
    {
        Assert.Throws<KeyNotFoundException>(() => _store.GetState("wf-nonexistent-20260312-xyz"));
    }

    // 8. List active states
    [Fact]
    public void ListActiveStates_ShouldReturnOnlyRunningWorkflows()
    {
        _store.SaveState(CreateState("wf-1", "wf-a"));
        _store.SaveState(CreateState("wf-2", "wf-b"));
        _store.SaveState(CreateState("wf-3", "wf-c"));

        _store.UpdateState("wf-1", "step-1", WorkflowInstanceStatus.Running);
        _store.UpdateState("wf-2", "step-1", WorkflowInstanceStatus.Running);
        _store.UpdateState("wf-3", "step-1", WorkflowInstanceStatus.Completed);

        var active = _store.ListActiveStates();

        Assert.Equal(2, active.Count);
    }
}
