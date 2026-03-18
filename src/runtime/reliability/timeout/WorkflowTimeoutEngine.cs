namespace Whycespace.Runtime.Reliability.Timeout;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Runtime.Persistence.Abstractions;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

/// <summary>
/// Local command model for the WorkflowTimeoutEngine's EvaluateTimeout method.
/// </summary>
public sealed record WorkflowTimeoutCommand(
    string InstanceId,
    string StepId,
    TimeSpan Timeout,
    DateTimeOffset StartedAt
);

/// <summary>
/// Local result model for the WorkflowTimeoutEngine's EvaluateTimeout method.
/// </summary>
public sealed record WorkflowTimeoutResult(
    string InstanceId,
    string StepId,
    bool IsTimedOut,
    TimeSpan Elapsed,
    TimeSpan Timeout
)
{
    public static WorkflowTimeoutResult Ok(
        string instanceId,
        string stepId,
        bool isTimedOut,
        TimeSpan elapsed,
        TimeSpan timeout)
        => new(instanceId, stepId, isTimedOut, elapsed, timeout);
}

[EngineManifest("WorkflowTimeoutEngine", EngineTier.T1M, EngineKind.Decision, "WorkflowTimeoutRequest", typeof(EngineEvent))]
public sealed class WorkflowTimeoutEngine : IEngine, Whycespace.WorkflowRuntime.IWorkflowTimeoutEngine
{
    private readonly IWorkflowTimeoutStore _timeoutStore;

    public string Name => "WorkflowTimeoutEngine";

    public WorkflowTimeoutEngine(IWorkflowTimeoutStore timeoutStore)
    {
        _timeoutStore = timeoutStore;
    }

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "evaluate" => HandleEvaluate(context),
            "registerStep" => HandleRegisterStep(context),
            "registerWorkflow" => HandleRegisterWorkflow(context),
            "checkStep" => HandleCheckStep(context),
            "checkWorkflow" => HandleCheckWorkflow(context),
            "clear" => HandleClear(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'"))
        };
    }

    public void RegisterStepTimeout(string instanceId, string stepId, TimeSpan timeout)
    {
        var entry = new TimeoutEntry(instanceId, stepId, DateTimeOffset.UtcNow, timeout);
        _timeoutStore.RegisterTimeout(instanceId, stepId, entry);
    }

    public void RegisterWorkflowTimeout(string instanceId, TimeSpan timeout)
    {
        var entry = new TimeoutEntry(instanceId, "__workflow__", DateTimeOffset.UtcNow, timeout);
        _timeoutStore.RegisterTimeout(instanceId, "__workflow__", entry);
    }

    public TimeoutDecision CheckStepTimeout(string instanceId, string stepId)
    {
        return CheckTimeout(instanceId, stepId);
    }

    public TimeoutDecision CheckWorkflowTimeout(string instanceId)
    {
        return CheckTimeout(instanceId, "__workflow__");
    }

    public void ClearTimeout(string instanceId, string stepId)
    {
        _timeoutStore.RemoveTimeout(instanceId, stepId);
    }

    private TimeoutDecision CheckTimeout(string instanceId, string stepId)
    {
        var entry = _timeoutStore.GetTimeout(instanceId, stepId);
        if (entry is null)
            return new TimeoutDecision(false, instanceId, stepId, TimeSpan.Zero, TimeSpan.Zero);

        var elapsed = DateTimeOffset.UtcNow - entry.StartTime;
        var exceeded = elapsed > entry.TimeoutDuration;
        var exceededBy = exceeded ? elapsed - entry.TimeoutDuration : TimeSpan.Zero;

        return new TimeoutDecision(exceeded, instanceId, stepId, entry.TimeoutDuration, exceededBy);
    }

    public WorkflowTimeoutResult EvaluateTimeout(WorkflowTimeoutCommand command)
    {
        var elapsed = DateTimeOffset.UtcNow - command.StartedAt;
        var exceeded = elapsed > command.Timeout;

        return WorkflowTimeoutResult.Ok(
            command.InstanceId,
            command.StepId,
            exceeded,
            elapsed,
            command.Timeout);
    }

    private Task<EngineResult> HandleEvaluate(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var stepId = context.Data.GetValueOrDefault("stepId") as string ?? "";
        var timeoutMs = context.Data.GetValueOrDefault("timeoutMs") is double ms ? ms : 30000;
        var startedAtStr = context.Data.GetValueOrDefault("startedAt") as string;
        var startedAt = startedAtStr != null ? DateTimeOffset.Parse(startedAtStr) : DateTimeOffset.UtcNow;

        var command = new WorkflowTimeoutCommand(instanceId, stepId, TimeSpan.FromMilliseconds(timeoutMs), startedAt);
        var result = EvaluateTimeout(command);

        var events = result.IsTimedOut
            ? new[] { EngineEvent.Create("WorkflowStepTimedOut", Guid.Parse(context.WorkflowId), new Dictionary<string, object> { ["instanceId"] = instanceId, ["stepId"] = stepId }) }
            : Array.Empty<EngineEvent>();

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["instanceId"] = result.InstanceId,
            ["stepId"] = result.StepId,
            ["isTimedOut"] = result.IsTimedOut,
            ["elapsed"] = result.Elapsed.TotalMilliseconds,
            ["timeout"] = result.Timeout.TotalMilliseconds
        }));
    }

    private Task<EngineResult> HandleRegisterStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var stepId = context.Data.GetValueOrDefault("stepId") as string ?? "";
        var timeoutMs = context.Data.GetValueOrDefault("timeoutMs") is double ms ? ms : 30000;
        RegisterStepTimeout(instanceId, stepId, TimeSpan.FromMilliseconds(timeoutMs));
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object> { ["registered"] = true }));
    }

    private Task<EngineResult> HandleRegisterWorkflow(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var timeoutMs = context.Data.GetValueOrDefault("timeoutMs") is double ms ? ms : 300000;
        RegisterWorkflowTimeout(instanceId, TimeSpan.FromMilliseconds(timeoutMs));
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object> { ["registered"] = true }));
    }

    private Task<EngineResult> HandleCheckStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var stepId = context.Data.GetValueOrDefault("stepId") as string ?? "";
        var decision = CheckStepTimeout(instanceId, stepId);
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["exceeded"] = decision.IsTimeout,
            ["elapsed"] = decision.ExceededBy.TotalMilliseconds,
            ["duration"] = decision.TimeoutDuration.TotalMilliseconds
        }));
    }

    private Task<EngineResult> HandleCheckWorkflow(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var decision = CheckWorkflowTimeout(instanceId);
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["exceeded"] = decision.IsTimeout,
            ["elapsed"] = decision.ExceededBy.TotalMilliseconds,
            ["duration"] = decision.TimeoutDuration.TotalMilliseconds
        }));
    }

    private Task<EngineResult> HandleClear(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";
        var stepId = context.Data.GetValueOrDefault("stepId") as string ?? "";
        ClearTimeout(instanceId, stepId);
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object> { ["cleared"] = true }));
    }
}
