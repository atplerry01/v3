namespace Whycespace.Engines.T2E.HEOS;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Operators;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkforceLifecycle", EngineTier.T2E, EngineKind.Mutation, "WorkforceLifecycleCommand", typeof(EngineEvent))]
public sealed class WorkforceLifecycleEngine : IEngine
{
    public string Name => "WorkforceLifecycle";

    private static readonly Dictionary<(WorkerStatus From, LifecycleAction Action), WorkerStatus> AllowedTransitions = new()
    {
        { (WorkerStatus.Registered, LifecycleAction.Activate), WorkerStatus.Active },
        { (WorkerStatus.Active, LifecycleAction.SetUnavailable), WorkerStatus.Unavailable },
        { (WorkerStatus.Active, LifecycleAction.Suspend), WorkerStatus.Suspended },
        { (WorkerStatus.Active, LifecycleAction.Terminate), WorkerStatus.Terminated },
        { (WorkerStatus.Unavailable, LifecycleAction.Activate), WorkerStatus.Active },
        { (WorkerStatus.Suspended, LifecycleAction.Reactivate), WorkerStatus.Active },
    };

    private static readonly Dictionary<LifecycleAction, string> RequiredScopes = new()
    {
        { LifecycleAction.Suspend, "heos.workforce.suspend" },
        { LifecycleAction.Terminate, "heos.workforce.terminate" },
        { LifecycleAction.Reactivate, "heos.workforce.reactivate" },
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid lifecycle command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var operatorAgg = ResolveOperator(context);
        if (operatorAgg is null)
            return Task.FromResult(EngineResult.Fail("Missing operator aggregate data"));

        var decision = ProcessLifecycle(workforce, operatorAgg, command);

        if (!decision.Success)
            return Task.FromResult(EngineResult.Fail(decision.Reason));

        var events = new[]
        {
            EngineEvent.Create("WorkforceLifecycleTransitioned", command.WorkforceId,
                new Dictionary<string, object>
                {
                    ["workforceId"] = command.WorkforceId.ToString(),
                    ["action"] = command.LifecycleAction.ToString(),
                    ["previousStatus"] = decision.PreviousStatus,
                    ["newStatus"] = decision.NewStatus,
                    ["requestedByOperatorId"] = command.RequestedByOperatorId.ToString(),
                    ["reason"] = command.Reason,
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["success"] = decision.Success,
                ["previousStatus"] = decision.PreviousStatus,
                ["newStatus"] = decision.NewStatus,
                ["reason"] = decision.Reason
            }));
    }

    public static WorkforceLifecycleDecision ProcessLifecycle(
        WorkforceAggregate workforce,
        OperatorAggregate operatorAgg,
        WorkforceLifecycleCommand command)
    {
        var currentStatus = workforce.Status;

        if (!operatorAgg.IsActive())
            return WorkforceLifecycleDecision.Rejected(
                currentStatus.ToString(), "Operator is not active", command.Timestamp);

        if (RequiredScopes.TryGetValue(command.LifecycleAction, out var requiredScope))
        {
            if (!operatorAgg.HasAuthorityForScope(requiredScope))
                return WorkforceLifecycleDecision.Rejected(
                    currentStatus.ToString(),
                    $"Operator lacks required authority '{requiredScope}' for {command.LifecycleAction}",
                    command.Timestamp);
        }

        var transitionKey = (currentStatus, command.LifecycleAction);
        if (!AllowedTransitions.TryGetValue(transitionKey, out var newStatus))
            return WorkforceLifecycleDecision.Rejected(
                currentStatus.ToString(),
                $"Invalid lifecycle transition: {currentStatus} → {command.LifecycleAction}",
                command.Timestamp);

        return WorkforceLifecycleDecision.Accepted(
            currentStatus.ToString(), newStatus.ToString(), command.Timestamp);
    }

    private static WorkforceLifecycleCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        var action = context.Data.GetValueOrDefault("lifecycleAction") as string;
        var operatorId = context.Data.GetValueOrDefault("requestedByOperatorId") as string;
        var reason = context.Data.GetValueOrDefault("reason") as string;
        var timestamp = ResolveDateTime(context.Data.GetValueOrDefault("timestamp"));

        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;
        if (string.IsNullOrEmpty(action) || !Enum.TryParse<LifecycleAction>(action, true, out var lifecycleAction))
            return null;
        if (string.IsNullOrEmpty(operatorId) || !Guid.TryParse(operatorId, out var opGuid))
            return null;
        if (string.IsNullOrEmpty(reason))
            return null;
        if (timestamp is null)
            return null;

        return new WorkforceLifecycleCommand(wfGuid, lifecycleAction, opGuid, reason, timestamp.Value);
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var capabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();
        var status = context.Data.GetValueOrDefault("workerStatus") as string ?? "Active";

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        var workforce = WorkforceAggregate.Register(new WorkerId(wGuid), workerName, capabilities);

        if (Enum.TryParse<WorkerStatus>(status, true, out var workerStatus) && workerStatus != WorkerStatus.Active)
            workforce.SetStatus(workerStatus);

        return workforce;
    }

    private static OperatorAggregate? ResolveOperator(EngineContext context)
    {
        var operatorId = context.Data.GetValueOrDefault("requestedByOperatorId") as string;
        var operatorName = context.Data.GetValueOrDefault("operatorName") as string ?? "Operator";
        var scopes = context.Data.GetValueOrDefault("operatorScopes") as IEnumerable<string>
            ?? Array.Empty<string>();
        var operatorStatus = context.Data.GetValueOrDefault("operatorStatus") as string ?? "Active";

        if (string.IsNullOrEmpty(operatorId) || !Guid.TryParse(operatorId, out var oGuid))
            return null;

        var operatorAgg = OperatorAggregate.Register(new OperatorId(oGuid), operatorName, scopes);

        if (operatorStatus == "Suspended")
            operatorAgg.Suspend();

        return operatorAgg;
    }

    private static DateTimeOffset? ResolveDateTime(object? value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            string s when DateTimeOffset.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
