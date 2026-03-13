namespace Whycespace.Engines.T2E.HEOS;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Operators;
using Whycespace.Domain.Core.Workforce;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("HEOSAssignment", EngineTier.T2E, EngineKind.Mutation, "AssignmentCommand", typeof(EngineEvent))]
public sealed class AssignmentEngine : IEngine
{
    public string Name => "HEOSAssignment";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid assignment command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var operatorAgg = ResolveOperator(context);
        if (operatorAgg is null)
            return Task.FromResult(EngineResult.Fail("Missing operator aggregate data"));

        var decision = AssignWorker(workforce, operatorAgg, command);

        if (!decision.Assigned)
            return Task.FromResult(EngineResult.Fail(decision.Reason));

        var events = new[]
        {
            EngineEvent.Create("WorkerAssigned", decision.WorkerId,
                new Dictionary<string, object>
                {
                    ["workerId"] = decision.WorkerId.ToString(),
                    ["taskId"] = decision.TaskId.ToString(),
                    ["taskType"] = command.TaskType,
                    ["assignmentScope"] = command.AssignmentScope,
                    ["requestedByOperatorId"] = command.RequestedByOperatorId.ToString(),
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["assigned"] = decision.Assigned,
                ["workerId"] = decision.WorkerId.ToString(),
                ["taskId"] = decision.TaskId.ToString(),
                ["reason"] = decision.Reason
            }));
    }

    public static AssignmentDecision AssignWorker(
        WorkforceAggregate workforce,
        OperatorAggregate operatorAgg,
        AssignmentCommand command)
    {
        if (!workforce.IsEligible())
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                "Worker is not active");

        if (!workforce.IsAvailable())
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                "Worker is not available");

        if (!workforce.HasCapability(command.TaskType))
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                $"Worker does not have required capability '{command.TaskType}'");

        if (!operatorAgg.IsActive())
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                "Operator is not active");

        if (!operatorAgg.HasAuthorityForScope(command.AssignmentScope))
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                $"Operator does not have authority for scope '{command.AssignmentScope}'");

        if (workforce.IsAssignedTo(command.TaskId))
            return AssignmentDecision.Rejected(workforce.WorkerId, command.TaskId,
                "Worker is already assigned to this task");

        return AssignmentDecision.Success(workforce.WorkerId, command.TaskId);
    }

    private static AssignmentCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        var taskId = context.Data.GetValueOrDefault("taskId") as string;
        var taskType = context.Data.GetValueOrDefault("taskType") as string;
        var operatorId = context.Data.GetValueOrDefault("requestedByOperatorId") as string;
        var scope = context.Data.GetValueOrDefault("assignmentScope") as string;

        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;
        if (string.IsNullOrEmpty(taskId) || !Guid.TryParse(taskId, out var tGuid))
            return null;
        if (string.IsNullOrEmpty(taskType))
            return null;
        if (string.IsNullOrEmpty(operatorId) || !Guid.TryParse(operatorId, out var opGuid))
            return null;
        if (string.IsNullOrEmpty(scope))
            return null;

        return new AssignmentCommand(wfGuid, tGuid, taskType, opGuid, scope);
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var capabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();
        var status = context.Data.GetValueOrDefault("workerStatus") as string ?? "Active";
        var availability = context.Data.GetValueOrDefault("workerAvailability") as string ?? "Available";
        var currentTaskId = context.Data.GetValueOrDefault("workerCurrentTaskId") as string;

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        var workforce = WorkforceAggregate.Register(new WorkerId(wGuid), workerName, capabilities);

        if (status == "Suspended")
            workforce.Suspend();

        if (availability == "Busy" && Guid.TryParse(currentTaskId, out var ctGuid))
            workforce.AssignToTask(ctGuid);

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
}
