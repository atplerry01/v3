namespace Whycespace.Runtime.Reliability.Retry;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.WorkflowRuntime;

[EngineManifest("WorkflowRetryPolicy", EngineTier.T1M, EngineKind.Decision, "WorkflowRetryPolicyCommand", typeof(EngineEvent))]
public sealed class WorkflowRetryPolicyEngine : IEngine, Whycespace.WorkflowRuntime.IWorkflowRetryPolicyEngine
{
    private readonly Whycespace.Runtime.Persistence.Workflow.IWorkflowRetryStore? _retryStore;

    public WorkflowRetryPolicyEngine() { }

    public WorkflowRetryPolicyEngine(Whycespace.Runtime.Persistence.Workflow.IWorkflowRetryStore retryStore)
    {
        _retryStore = retryStore;
    }

    public string Name => "WorkflowRetryPolicy";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var instanceId = context.Data.GetValueOrDefault("workflowInstanceId") as string;
        var stepId = context.Data.GetValueOrDefault("stepId") as string;
        if (instanceId is null || stepId is null)
            return Task.FromResult(EngineResult.Fail("Missing required retry policy parameters"));

        var maxRetries = context.Data.TryGetValue("maxRetries", out var mrVal) && mrVal is int mr ? mr : 3;
        var strategyStr = context.Data.GetValueOrDefault("retryStrategy") as string ?? "FixedDelay";
        var strategy = Enum.TryParse<RetryStrategy>(strategyStr, true, out var s) ? s : RetryStrategy.FixedDelay;
        var initialDelayMs = context.Data.TryGetValue("initialDelayMs", out var idVal) && idVal is double idMs ? idMs : 1000;
        var backoffMultiplier = context.Data.TryGetValue("backoffMultiplier", out var bmVal) && bmVal is double bm ? bm : 2.0;
        var currentRetryCount = context.Data.TryGetValue("currentRetryCount", out var rcVal) && rcVal is int rc ? rc : 0;
        var lastFailureStr = context.Data.GetValueOrDefault("lastFailureTimestamp") as string;
        var lastFailure = lastFailureStr != null ? DateTimeOffset.Parse(lastFailureStr) : DateTimeOffset.UtcNow;

        var retryPolicy = new RetryPolicy(maxRetries, strategy, TimeSpan.FromMilliseconds(initialDelayMs), backoffMultiplier);
        var command = new WorkflowRetryPolicyCommand(instanceId, stepId, retryPolicy, currentRetryCount, lastFailure);

        var result = EvaluateRetryPolicy(command);
        var events = BuildEvents(context, result);
        var output = BuildOutput(result);

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    WorkflowRetryPolicyResult IWorkflowRetryPolicyEngine.EvaluateRetryPolicy(WorkflowRetryPolicyCommand command)
    {
        return EvaluateRetryPolicy(command);
    }

    public static WorkflowRetryPolicyResult EvaluateRetryPolicy(WorkflowRetryPolicyCommand command)
    {
        var retryAllowed = command.CurrentRetryCount < command.RetryPolicy.MaxRetries;

        var delay = retryAllowed
            ? command.RetryPolicy.RetryStrategy switch
            {
                RetryStrategy.FixedDelay => command.RetryPolicy.InitialDelay,
                RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(
                    command.RetryPolicy.InitialDelay.TotalMilliseconds * Math.Pow(command.RetryPolicy.BackoffMultiplier, command.CurrentRetryCount)),
                RetryStrategy.Immediate => TimeSpan.Zero,
                _ => command.RetryPolicy.InitialDelay
            }
            : TimeSpan.Zero;

        return new WorkflowRetryPolicyResult(
            command.WorkflowInstanceId,
            command.StepId,
            retryAllowed,
            delay,
            command.CurrentRetryCount,
            DateTimeOffset.UtcNow);
    }

    private static EngineEvent[] BuildEvents(EngineContext context, WorkflowRetryPolicyResult result)
    {
        var eventType = result.RetryAllowed ? "WorkflowStepRetryApproved" : "WorkflowStepRetryDenied";

        return new[]
        {
            EngineEvent.Create(eventType, Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["instanceId"] = result.WorkflowInstanceId,
                    ["stepId"] = result.StepId,
                    ["retryAllowed"] = result.RetryAllowed,
                    ["retryDelay"] = result.RetryDelay.TotalMilliseconds,
                    ["retryCount"] = result.RetryCount
                })
        };
    }

    private static Dictionary<string, object> BuildOutput(WorkflowRetryPolicyResult result)
    {
        return new Dictionary<string, object>
        {
            ["instanceId"] = result.WorkflowInstanceId,
            ["stepId"] = result.StepId,
            ["retryAllowed"] = result.RetryAllowed,
            ["retryDelay"] = result.RetryDelay.TotalMilliseconds,
            ["retryCount"] = result.RetryCount,
            ["decisionTimestamp"] = result.DecisionTimestamp.ToString("O")
        };
    }

    public int GetRetryCount(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("Retry store not configured");
        return _retryStore.GetRetryCount(instanceId, stepId);
    }

    public void RegisterRetryAttempt(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("Retry store not configured");
        _retryStore.IncrementRetryCount(instanceId, stepId);
    }

    public void ResetRetryCount(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("Retry store not configured");
        _retryStore.ResetRetryCount(instanceId, stepId);
    }
}
