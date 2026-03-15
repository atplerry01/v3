namespace Whycespace.Engines.T1M.WSS.Lifecycle;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest(
    "WorkflowInstanceLifecycle",
    EngineTier.T1M,
    EngineKind.Decision,
    "WorkflowLifecycleCommand",
    typeof(EngineEvent))]
public sealed class WorkflowInstanceLifecycleEngine : IEngine
{
    public string Name => "WorkflowInstanceLifecycle";

    private static readonly Dictionary<(WorkflowLifecycleStatus From, WorkflowLifecycleTransition Via), WorkflowLifecycleStatus> TransitionMatrix = new()
    {
        { (WorkflowLifecycleStatus.Created, WorkflowLifecycleTransition.Start), WorkflowLifecycleStatus.Running },
        { (WorkflowLifecycleStatus.Running, WorkflowLifecycleTransition.Complete), WorkflowLifecycleStatus.Completed },
        { (WorkflowLifecycleStatus.Running, WorkflowLifecycleTransition.Fail), WorkflowLifecycleStatus.Failed },
        { (WorkflowLifecycleStatus.Running, WorkflowLifecycleTransition.Terminate), WorkflowLifecycleStatus.Terminated },
        { (WorkflowLifecycleStatus.Waiting, WorkflowLifecycleTransition.Start), WorkflowLifecycleStatus.Running },
        { (WorkflowLifecycleStatus.Failed, WorkflowLifecycleTransition.Recover), WorkflowLifecycleStatus.Retrying },
        { (WorkflowLifecycleStatus.Failed, WorkflowLifecycleTransition.Terminate), WorkflowLifecycleStatus.Terminated },
        { (WorkflowLifecycleStatus.Retrying, WorkflowLifecycleTransition.Start), WorkflowLifecycleStatus.Running },
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = WorkflowLifecycleCommand.FromContextData(context.Data);
        var result = EvaluateTransition(command);

        if (!result.TransitionAccepted)
        {
            var failEvent = EngineEvent.Create(
                "WorkflowLifecycleTransitionRejected",
                Guid.TryParse(command.WorkflowInstanceId, out var failId) ? failId : Guid.Empty,
                new Dictionary<string, object>
                {
                    ["workflowInstanceId"] = command.WorkflowInstanceId,
                    ["currentStatus"] = result.PreviousStatus.ToString(),
                    ["requestedTransition"] = command.RequestedTransition.ToString(),
                    ["reason"] = result.TransitionReason,
                    ["evaluatedAt"] = result.EvaluatedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.wss.workflow.lifecycle"
                });

            return Task.FromResult(new EngineResult(false, new[] { failEvent }, new Dictionary<string, object>
            {
                ["workflowInstanceId"] = result.WorkflowInstanceId,
                ["transitionAccepted"] = false,
                ["reason"] = result.TransitionReason
            }));
        }

        var successEvent = EngineEvent.Create(
            "WorkflowLifecycleTransitionAccepted",
            Guid.TryParse(command.WorkflowInstanceId, out var successId) ? successId : Guid.Empty,
            new Dictionary<string, object>
            {
                ["workflowInstanceId"] = command.WorkflowInstanceId,
                ["previousStatus"] = result.PreviousStatus.ToString(),
                ["newStatus"] = result.NewStatus.ToString(),
                ["requestedTransition"] = command.RequestedTransition.ToString(),
                ["reason"] = result.TransitionReason,
                ["evaluatedAt"] = result.EvaluatedAt.ToString("O"),
                ["eventVersion"] = 1,
                ["topic"] = "whyce.wss.workflow.lifecycle"
            });

        return Task.FromResult(EngineResult.Ok(new[] { successEvent }, new Dictionary<string, object>
        {
            ["workflowInstanceId"] = result.WorkflowInstanceId,
            ["previousStatus"] = result.PreviousStatus.ToString(),
            ["newStatus"] = result.NewStatus.ToString(),
            ["transitionAccepted"] = true,
            ["reason"] = result.TransitionReason
        }));
    }

    public WorkflowLifecycleResult EvaluateTransition(WorkflowLifecycleCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.WorkflowInstanceId))
        {
            return WorkflowLifecycleResult.Rejected(
                command.WorkflowInstanceId,
                command.CurrentStatus,
                "WorkflowInstanceId must not be empty.",
                command.Timestamp);
        }

        var key = (command.CurrentStatus, command.RequestedTransition);

        if (!TransitionMatrix.TryGetValue(key, out var newStatus))
        {
            return WorkflowLifecycleResult.Rejected(
                command.WorkflowInstanceId,
                command.CurrentStatus,
                $"Transition '{command.RequestedTransition}' is not allowed from status '{command.CurrentStatus}'.",
                command.Timestamp);
        }

        return WorkflowLifecycleResult.Accepted(
            command.WorkflowInstanceId,
            command.CurrentStatus,
            newStatus,
            $"Transition '{command.RequestedTransition}' accepted: {command.CurrentStatus} → {newStatus}.",
            command.Timestamp);
    }
}
