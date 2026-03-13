namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Models;

[EngineManifest("WorkflowRetryPolicyEngine", EngineTier.T1M, EngineKind.Decision, "WorkflowRetryPolicyRequest", typeof(EngineEvent))]
public sealed class WorkflowRetryPolicyEngine : IEngine, IWorkflowRetryPolicyEngine
{
    private readonly IWorkflowRetryStore _retryStore;

    public string Name => "WorkflowRetryPolicyEngine";

    public WorkflowRetryPolicyEngine(IWorkflowRetryStore retryStore)
    {
        _retryStore = retryStore;
    }

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "evaluate" => HandleEvaluate(context),
            "register" => HandleRegister(context),
            "get" => HandleGetRetryCount(context),
            "reset" => HandleReset(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: evaluate, register, get, reset"))
        };
    }

    public RetryDecision EvaluateRetryPolicy(WorkflowFailurePolicy policy, int currentRetryCount)
    {
        return policy.Action switch
        {
            FailureAction.Retry when currentRetryCount < policy.MaxRetries =>
                new RetryDecision(true, policy.RetryDelay, FailureAction.Retry),

            FailureAction.Retry =>
                new RetryDecision(false, TimeSpan.Zero, FailureAction.Fail),

            FailureAction.Skip =>
                new RetryDecision(false, TimeSpan.Zero, FailureAction.Skip),

            FailureAction.Compensate =>
                new RetryDecision(false, TimeSpan.Zero, FailureAction.Compensate),

            _ =>
                new RetryDecision(false, TimeSpan.Zero, FailureAction.Fail)
        };
    }

    public void RegisterRetryAttempt(string instanceId, string stepId)
    {
        _retryStore.IncrementRetryCount(instanceId, stepId);
    }

    public int GetRetryCount(string instanceId, string stepId)
    {
        return _retryStore.GetRetryCount(instanceId, stepId);
    }

    public void ResetRetryCount(string instanceId, string stepId)
    {
        _retryStore.ResetRetryCount(instanceId, stepId);
    }

    private Task<EngineResult> HandleEvaluate(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;
        var actionStr = context.Data.GetValueOrDefault("failureAction") as string;
        var maxRetriesObj = context.Data.GetValueOrDefault("maxRetries");
        var retryDelaySecondsObj = context.Data.GetValueOrDefault("retryDelaySeconds");

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        if (!Enum.TryParse<FailureAction>(actionStr, true, out var failureAction))
            return Task.FromResult(EngineResult.Fail($"Invalid failureAction: '{actionStr}'"));

        var maxRetries = maxRetriesObj is int mr ? mr : 3;
        var retryDelaySeconds = retryDelaySecondsObj is int rds ? rds : (retryDelaySecondsObj is double rdd ? (int)rdd : 5);

        var policy = new WorkflowFailurePolicy(failureAction, maxRetries, TimeSpan.FromSeconds(retryDelaySeconds), null);
        var currentCount = _retryStore.GetRetryCount(instanceId, stepId);
        var decision = EvaluateRetryPolicy(policy, currentCount);

        var events = new[]
        {
            EngineEvent.Create("WorkflowRetryEvaluated", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["instanceId"] = instanceId,
                    ["stepId"] = stepId,
                    ["shouldRetry"] = decision.ShouldRetry,
                    ["failureAction"] = decision.FailureAction.ToString()
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["shouldRetry"] = decision.ShouldRetry,
            ["retryDelay"] = decision.RetryDelay.TotalSeconds,
            ["failureAction"] = decision.FailureAction.ToString(),
            ["currentRetryCount"] = currentCount
        }));
    }

    private Task<EngineResult> HandleRegister(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        RegisterRetryAttempt(instanceId, stepId);
        var count = GetRetryCount(instanceId, stepId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = count
        }));
    }

    private Task<EngineResult> HandleGetRetryCount(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        var count = GetRetryCount(instanceId, stepId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = count
        }));
    }

    private Task<EngineResult> HandleReset(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing instanceId or stepId"));

        ResetRetryCount(instanceId, stepId);

        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = 0
        }));
    }
}
