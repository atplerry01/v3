namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Models;

[EngineManifest("WorkflowTimeoutEngine", EngineTier.T1M, EngineKind.Decision, "WorkflowTimeoutRequest", typeof(EngineEvent))]
public sealed class WorkflowTimeoutEngine : IEngine, IWorkflowTimeoutEngine
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
            "registerStep" => HandleRegisterStep(context),
            "registerWorkflow" => HandleRegisterWorkflow(context),
            "checkStep" => HandleCheckStep(context),
            "checkWorkflow" => HandleCheckWorkflow(context),
            "clear" => HandleClear(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: registerStep, registerWorkflow, checkStep, checkWorkflow, clear"))
        };
    }

    public void RegisterStepTimeout(string instanceId, string stepId, TimeSpan timeout)
    {
        var entry = new TimeoutEntry(instanceId, stepId, DateTimeOffset.UtcNow, timeout);
        _timeoutStore.RegisterTimeout(instanceId, stepId, entry);
    }

    public void RegisterWorkflowTimeout(string instanceId, TimeSpan timeout)
    {
        var entry = new TimeoutEntry(instanceId, "workflow", DateTimeOffset.UtcNow, timeout);
        _timeoutStore.RegisterTimeout(instanceId, "workflow", entry);
    }

    public TimeoutDecision CheckStepTimeout(string instanceId, string stepId)
    {
        return CheckTimeout(instanceId, stepId);
    }

    public TimeoutDecision CheckWorkflowTimeout(string instanceId)
    {
        return CheckTimeout(instanceId, "workflow");
    }

    public void ClearTimeout(string instanceId, string stepId)
    {
        _timeoutStore.RemoveTimeout(instanceId, stepId);
    }

    private TimeoutDecision CheckTimeout(string instanceId, string stepId)
    {
        var entry = _timeoutStore.GetTimeout(instanceId, stepId);

        if (entry is null)
        {
            return new TimeoutDecision(false, instanceId, stepId, TimeSpan.Zero, TimeSpan.Zero);
        }

        var elapsed = DateTimeOffset.UtcNow - entry.StartTime;
        var exceeded = elapsed - entry.TimeoutDuration;

        if (exceeded > TimeSpan.Zero)
        {
            return new TimeoutDecision(true, instanceId, stepId, entry.TimeoutDuration, exceeded);
        }

        return new TimeoutDecision(false, instanceId, stepId, entry.TimeoutDuration, TimeSpan.Zero);
    }

    private Task<EngineResult> HandleRegisterStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;
        var timeoutSecondsObj = context.Data.GetValueOrDefault("timeoutSeconds");

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        var timeoutSeconds = timeoutSecondsObj is int ts ? ts : (timeoutSecondsObj is double td ? (int)td : 30);
        RegisterStepTimeout(instanceId, stepId, TimeSpan.FromSeconds(timeoutSeconds));

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["timeoutSeconds"] = timeoutSeconds
        }));
    }

    private Task<EngineResult> HandleRegisterWorkflow(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var timeoutSecondsObj = context.Data.GetValueOrDefault("timeoutSeconds");

        if (string.IsNullOrWhiteSpace(instanceId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId"));

        var timeoutSeconds = timeoutSecondsObj is int ts ? ts : (timeoutSecondsObj is double td ? (int)td : 300);
        RegisterWorkflowTimeout(instanceId, TimeSpan.FromSeconds(timeoutSeconds));

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["timeoutSeconds"] = timeoutSeconds
        }));
    }

    private Task<EngineResult> HandleCheckStep(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        var decision = CheckStepTimeout(instanceId, stepId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["isTimeout"] = decision.IsTimeout,
            ["instanceId"] = decision.InstanceId,
            ["stepId"] = decision.StepId,
            ["timeoutDuration"] = decision.TimeoutDuration.TotalSeconds,
            ["exceededBy"] = decision.ExceededBy.TotalSeconds
        }));
    }

    private Task<EngineResult> HandleCheckWorkflow(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(instanceId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId"));

        var decision = CheckWorkflowTimeout(instanceId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["isTimeout"] = decision.IsTimeout,
            ["instanceId"] = decision.InstanceId,
            ["stepId"] = decision.StepId,
            ["timeoutDuration"] = decision.TimeoutDuration.TotalSeconds,
            ["exceededBy"] = decision.ExceededBy.TotalSeconds
        }));
    }

    private Task<EngineResult> HandleClear(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        ClearTimeout(instanceId, stepId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["cleared"] = true
        }));
    }
}
