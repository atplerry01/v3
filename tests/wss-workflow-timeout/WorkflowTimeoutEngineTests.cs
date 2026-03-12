using Whycespace.Engines.T1M.WSS.Runtime;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Models;

namespace Whycespace.WSS.WorkflowTimeout.Tests;

public class WorkflowTimeoutEngineTests
{
    private readonly WorkflowTimeoutEngine _engine;
    private readonly WorkflowTimeoutStore _store;

    public WorkflowTimeoutEngineTests()
    {
        _store = new WorkflowTimeoutStore();
        _engine = new WorkflowTimeoutEngine(_store);
    }

    // 1. Register step timeout
    [Fact]
    public void RegisterStepTimeout_ShouldStoreEntry()
    {
        _engine.RegisterStepTimeout("inst-1", "step-validate", TimeSpan.FromSeconds(30));

        var entry = _store.GetTimeout("inst-1", "step-validate");

        Assert.NotNull(entry);
        Assert.Equal("inst-1", entry.InstanceId);
        Assert.Equal("step-validate", entry.StepId);
        Assert.Equal(TimeSpan.FromSeconds(30), entry.TimeoutDuration);
    }

    // 2. Register workflow timeout
    [Fact]
    public void RegisterWorkflowTimeout_ShouldStoreEntry()
    {
        _engine.RegisterWorkflowTimeout("inst-2", TimeSpan.FromMinutes(5));

        var entry = _store.GetTimeout("inst-2", "workflow");

        Assert.NotNull(entry);
        Assert.Equal("inst-2", entry.InstanceId);
        Assert.Equal("workflow", entry.StepId);
        Assert.Equal(TimeSpan.FromMinutes(5), entry.TimeoutDuration);
    }

    // 3. Detect step timeout
    [Fact]
    public void CheckStepTimeout_WhenExpired_ShouldDetectTimeout()
    {
        // Register with a very short timeout and a past start time
        var entry = new TimeoutEntry("inst-3", "step-process", DateTimeOffset.UtcNow.AddSeconds(-10), TimeSpan.FromSeconds(5));
        _store.RegisterTimeout("inst-3", "step-process", entry);

        var decision = _engine.CheckStepTimeout("inst-3", "step-process");

        Assert.True(decision.IsTimeout);
        Assert.Equal("inst-3", decision.InstanceId);
        Assert.Equal("step-process", decision.StepId);
        Assert.True(decision.ExceededBy > TimeSpan.Zero);
    }

    // 4. Detect workflow timeout
    [Fact]
    public void CheckWorkflowTimeout_WhenExpired_ShouldDetectTimeout()
    {
        var entry = new TimeoutEntry("inst-4", "workflow", DateTimeOffset.UtcNow.AddSeconds(-20), TimeSpan.FromSeconds(10));
        _store.RegisterTimeout("inst-4", "workflow", entry);

        var decision = _engine.CheckWorkflowTimeout("inst-4");

        Assert.True(decision.IsTimeout);
        Assert.Equal("inst-4", decision.InstanceId);
        Assert.Equal("workflow", decision.StepId);
        Assert.True(decision.ExceededBy > TimeSpan.Zero);
    }

    // 5. Clear timeout
    [Fact]
    public void ClearTimeout_ShouldRemoveEntry()
    {
        _engine.RegisterStepTimeout("inst-5", "step-finalize", TimeSpan.FromSeconds(30));

        _engine.ClearTimeout("inst-5", "step-finalize");

        var entry = _store.GetTimeout("inst-5", "step-finalize");
        Assert.Null(entry);
    }

    // 6. No false timeout detection
    [Fact]
    public void CheckStepTimeout_WhenNotExpired_ShouldNotDetectTimeout()
    {
        _engine.RegisterStepTimeout("inst-6", "step-active", TimeSpan.FromMinutes(10));

        var decision = _engine.CheckStepTimeout("inst-6", "step-active");

        Assert.False(decision.IsTimeout);
        Assert.Equal("inst-6", decision.InstanceId);
        Assert.Equal("step-active", decision.StepId);
        Assert.Equal(TimeSpan.Zero, decision.ExceededBy);
    }
}
