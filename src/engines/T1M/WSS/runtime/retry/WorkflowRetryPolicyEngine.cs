namespace Whycespace.Engines.T1M.WSS.Runtime.Retry;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T1M.WSS.Workflows;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

/// <summary>
/// Evaluates retry policies for failed workflow steps.
/// Determines whether a failed step should retry, calculates delay using the configured strategy,
/// and enforces retry limits. Stateless and deterministic — retry state persistence is handled
/// by the Workflow State Store in the runtime layer.
/// </summary>
[EngineManifest("WorkflowRetryPolicy", EngineTier.T1M, EngineKind.Decision,
    "WorkflowRetryPolicyCommand", typeof(EngineEvent))]
public sealed class WorkflowRetryPolicyEngine : IEngine, IWorkflowRetryPolicyEngine
{
    private readonly Whycespace.Engines.T1M.WSS.Stores.IWorkflowRetryStore? _retryStore;

    public WorkflowRetryPolicyEngine() { }

    public WorkflowRetryPolicyEngine(Whycespace.Engines.T1M.WSS.Stores.IWorkflowRetryStore retryStore)
    {
        _retryStore = retryStore;
    }

    public string Name => "WorkflowRetryPolicy";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = WorkflowRetryPolicyCommand.FromContextData(context.Data);
        if (command is null)
            return Task.FromResult(EngineResult.Fail(
                "Invalid command: missing workflowInstanceId, stepId, or retryPolicy."));

        var result = EvaluateRetryPolicy(command);

        var events = BuildEvents(context, result);
        var output = BuildOutput(result);

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    /// <summary>
    /// Core retry policy evaluation logic. Stateless and deterministic.
    /// </summary>
    WorkflowRetryPolicyResult IWorkflowRetryPolicyEngine.EvaluateRetryPolicy(WorkflowRetryPolicyCommand command)
        => EvaluateRetryPolicy(command);

    public static WorkflowRetryPolicyResult EvaluateRetryPolicy(WorkflowRetryPolicyCommand command)
    {
        var policy = command.RetryPolicy;
        var retryAllowed = command.CurrentRetryCount < policy.MaxRetries;

        var retryDelay = retryAllowed
            ? policy.CalculateDelay(command.CurrentRetryCount)
            : TimeSpan.Zero;

        return new WorkflowRetryPolicyResult(
            command.WorkflowInstanceId,
            command.StepId,
            retryAllowed,
            retryDelay,
            command.CurrentRetryCount,
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<EngineEvent> BuildEvents(
        EngineContext context,
        WorkflowRetryPolicyResult result)
    {
        var aggregateId = Guid.TryParse(context.WorkflowId, out var parsed)
            ? parsed
            : Guid.NewGuid();

        var eventType = result.RetryAllowed
            ? "WorkflowStepRetryApproved"
            : "WorkflowStepRetryDenied";

        return new[]
        {
            EngineEvent.Create(eventType, aggregateId, new Dictionary<string, object>
            {
                ["workflowInstanceId"] = result.WorkflowInstanceId,
                ["stepId"] = result.StepId,
                ["retryAllowed"] = result.RetryAllowed,
                ["retryDelay"] = result.RetryDelay.TotalMilliseconds,
                ["retryCount"] = result.RetryCount,
                ["topic"] = "whyce.wss.workflow.retry.events"
            })
        };
    }

    private static IReadOnlyDictionary<string, object> BuildOutput(WorkflowRetryPolicyResult result)
    {
        return new Dictionary<string, object>
        {
            ["workflowInstanceId"] = result.WorkflowInstanceId,
            ["stepId"] = result.StepId,
            ["retryAllowed"] = result.RetryAllowed,
            ["retryDelay"] = result.RetryDelay.TotalMilliseconds,
            ["retryCount"] = result.RetryCount,
            ["decisionTimestamp"] = result.DecisionTimestamp
        };
    }

    public int GetRetryCount(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("RetryStore is not configured.");
        return _retryStore.GetRetryCount(instanceId, stepId);
    }

    public void RegisterRetryAttempt(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("RetryStore is not configured.");
        _retryStore.IncrementRetryCount(instanceId, stepId);
    }

    public void ResetRetryCount(string instanceId, string stepId)
    {
        if (_retryStore is null)
            throw new InvalidOperationException("RetryStore is not configured.");
        _retryStore.ResetRetryCount(instanceId, stepId);
    }
}
