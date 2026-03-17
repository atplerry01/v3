namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.Engines.T1M.WSS.Timeout;
using Whycespace.Engines.T1M.WSS.Workflows;
using Whycespace.Systems.Midstream.WSS.Models;

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
            "evaluate" => HandleEvaluate(context),
            "registerStep" => HandleRegisterStep(context),
            "registerWorkflow" => HandleRegisterWorkflow(context),
            "checkStep" => HandleCheckStep(context),
            "checkWorkflow" => HandleCheckWorkflow(context),
            "clear" => HandleClear(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: evaluate, registerStep, registerWorkflow, checkStep, checkWorkflow, clear"))
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

    /// <summary>
    /// Stateless, deterministic timeout evaluation.
    /// Takes a command with step start time, current timestamp, and timeout policy,
    /// and produces a result indicating whether the timeout threshold was exceeded.
    /// </summary>
    public WorkflowTimeoutResult EvaluateTimeout(WorkflowTimeoutCommand command)
    {
        var elapsed = command.CurrentTimestamp - command.StepStartedAt;
        var timedOut = elapsed > command.TimeoutPolicy.TimeoutDuration;

        return WorkflowTimeoutResult.Ok(
            command.WorkflowInstanceId,
            command.StepId,
            timedOut,
            elapsed,
            command.TimeoutPolicy.TimeoutDuration,
            command.CurrentTimestamp);
    }

    private Task<EngineResult> HandleEvaluate(EngineContext context)
    {
        var command = WorkflowTimeoutCommand.FromContextData(context.Data);

        if (string.IsNullOrWhiteSpace(command.WorkflowInstanceId))
            return Task.FromResult(EngineResult.Fail("Missing workflowInstanceId"));

        if (string.IsNullOrWhiteSpace(command.StepId))
            return Task.FromResult(EngineResult.Fail("Missing stepId"));

        var result = EvaluateTimeout(command);

        var aggregateId = Guid.TryParse(command.WorkflowInstanceId, out var parsed) ? parsed : Guid.Empty;

        var events = result.TimedOut
            ? new[]
            {
                EngineEvent.Create("WorkflowStepTimedOut", aggregateId,
                    new Dictionary<string, object>
                    {
                        ["workflowInstanceId"] = result.WorkflowInstanceId,
                        ["stepId"] = result.StepId,
                        ["elapsedSeconds"] = result.ElapsedTime.TotalSeconds,
                        ["timeoutThresholdSeconds"] = result.TimeoutThreshold.TotalSeconds,
                        ["timeoutStrategy"] = command.TimeoutPolicy.TimeoutStrategy.ToString(),
                        ["evaluatedAt"] = result.EvaluatedAt.ToString("O"),
                        ["eventVersion"] = 1,
                        ["topic"] = "whyce.wss.workflow.events"
                    })
            }
            : Array.Empty<EngineEvent>();

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["workflowInstanceId"] = result.WorkflowInstanceId,
            ["stepId"] = result.StepId,
            ["timedOut"] = result.TimedOut,
            ["elapsedSeconds"] = result.ElapsedTime.TotalSeconds,
            ["timeoutThresholdSeconds"] = result.TimeoutThreshold.TotalSeconds,
            ["evaluatedAt"] = result.EvaluatedAt.ToString("O")
        }));
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
