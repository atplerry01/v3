using Whycespace.Engines.T1M.Orchestration.Resilience;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Infrastructure.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

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

    // ── Store-based timeout management ──

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

    // ── Stateless timeout evaluation (command/result pattern) ──

    // 7. Step completes before timeout
    [Fact]
    public void EvaluateTimeout_StepCompletesBeforeTimeout_ShouldNotTimeout()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new WorkflowTimeoutCommand(
            "inst-7", "step-fast",
            now.AddSeconds(-5), now,
            new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(30), TimeoutStrategy.StepTimeout));

        var result = _engine.EvaluateTimeout(command);

        Assert.False(result.TimedOut);
        Assert.Equal("inst-7", result.WorkflowInstanceId);
        Assert.Equal("step-fast", result.StepId);
        Assert.Equal(TimeSpan.FromSeconds(5), result.ElapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(30), result.TimeoutThreshold);
    }

    // 8. Step exceeds timeout threshold
    [Fact]
    public void EvaluateTimeout_StepExceedsTimeout_ShouldTimeout()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new WorkflowTimeoutCommand(
            "inst-8", "step-slow",
            now.AddSeconds(-60), now,
            new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(30), TimeoutStrategy.StepTimeout));

        var result = _engine.EvaluateTimeout(command);

        Assert.True(result.TimedOut);
        Assert.Equal("inst-8", result.WorkflowInstanceId);
        Assert.Equal("step-slow", result.StepId);
        Assert.Equal(TimeSpan.FromSeconds(60), result.ElapsedTime);
        Assert.Equal(TimeSpan.FromSeconds(30), result.TimeoutThreshold);
    }

    // 9. Workflow timeout evaluation
    [Fact]
    public void EvaluateTimeout_WorkflowTimeout_ShouldEvaluateCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new WorkflowTimeoutCommand(
            "inst-9", "workflow",
            now.AddMinutes(-10), now,
            new WorkflowTimeoutPolicy(TimeSpan.FromMinutes(5), TimeoutStrategy.WorkflowTimeout));

        var result = _engine.EvaluateTimeout(command);

        Assert.True(result.TimedOut);
        Assert.Equal(TimeSpan.FromMinutes(10), result.ElapsedTime);
        Assert.Equal(TimeSpan.FromMinutes(5), result.TimeoutThreshold);
    }

    // 10. Timeout boundary condition (exactly at threshold)
    [Fact]
    public void EvaluateTimeout_ExactlyAtThreshold_ShouldNotTimeout()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new WorkflowTimeoutCommand(
            "inst-10", "step-boundary",
            now.AddSeconds(-30), now,
            new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(30), TimeoutStrategy.StepTimeout));

        var result = _engine.EvaluateTimeout(command);

        Assert.False(result.TimedOut);
        Assert.Equal(TimeSpan.FromSeconds(30), result.ElapsedTime);
    }

    // 11. Deterministic evaluation (same input produces same output)
    [Fact]
    public void EvaluateTimeout_SameInput_ProducesSameResult()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new WorkflowTimeoutCommand(
            "inst-11", "step-deterministic",
            now.AddSeconds(-15), now,
            new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(10), TimeoutStrategy.StepTimeout));

        var result1 = _engine.EvaluateTimeout(command);
        var result2 = _engine.EvaluateTimeout(command);

        Assert.Equal(result1.TimedOut, result2.TimedOut);
        Assert.Equal(result1.ElapsedTime, result2.ElapsedTime);
        Assert.Equal(result1.TimeoutThreshold, result2.TimeoutThreshold);
        Assert.Equal(result1.WorkflowInstanceId, result2.WorkflowInstanceId);
        Assert.Equal(result1.StepId, result2.StepId);
    }

    // 12. Concurrent timeout evaluations
    [Fact]
    public void EvaluateTimeout_ConcurrentEvaluations_ShouldBeThreadSafe()
    {
        var now = DateTimeOffset.UtcNow;
        var results = new WorkflowTimeoutResult[100];

        Parallel.For(0, 100, i =>
        {
            var command = new WorkflowTimeoutCommand(
                $"inst-concurrent-{i}", "step-parallel",
                now.AddSeconds(-20), now,
                new WorkflowTimeoutPolicy(TimeSpan.FromSeconds(10), TimeoutStrategy.StepTimeout));

            results[i] = _engine.EvaluateTimeout(command);
        });

        Assert.All(results, r =>
        {
            Assert.True(r.TimedOut);
            Assert.Equal(TimeSpan.FromSeconds(20), r.ElapsedTime);
            Assert.Equal(TimeSpan.FromSeconds(10), r.TimeoutThreshold);
        });
    }
}
