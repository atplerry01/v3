namespace Whycespace.Engines.T2E.HEOS;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Operators;
using Whycespace.Domain.Core.Workforce;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkforceScheduling", EngineTier.T2E, EngineKind.Mutation, "WorkforceScheduleCommand", typeof(EngineEvent))]
public sealed class WorkforceSchedulingEngine : IEngine
{
    public string Name => "WorkforceScheduling";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid schedule command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var operatorAgg = ResolveOperator(context);
        if (operatorAgg is null)
            return Task.FromResult(EngineResult.Fail("Missing operator aggregate data"));

        var decision = ScheduleWorkforce(workforce, operatorAgg, command);

        if (!decision.Scheduled)
            return Task.FromResult(EngineResult.Fail(decision.Reason));

        var events = new[]
        {
            EngineEvent.Create("WorkforceScheduled", decision.WorkforceId,
                new Dictionary<string, object>
                {
                    ["workforceId"] = decision.WorkforceId.ToString(),
                    ["taskId"] = decision.TaskId.ToString(),
                    ["taskType"] = command.TaskType,
                    ["scheduleStart"] = decision.ScheduleStart.ToString("O"),
                    ["scheduleEnd"] = decision.ScheduleEnd.ToString("O"),
                    ["scheduleScope"] = command.ScheduleScope,
                    ["operatorId"] = command.OperatorId.ToString(),
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["scheduled"] = decision.Scheduled,
                ["workforceId"] = decision.WorkforceId.ToString(),
                ["taskId"] = decision.TaskId.ToString(),
                ["scheduleStart"] = decision.ScheduleStart.ToString("O"),
                ["scheduleEnd"] = decision.ScheduleEnd.ToString("O"),
                ["reason"] = decision.Reason
            }));
    }

    public static WorkforceScheduleDecision ScheduleWorkforce(
        WorkforceAggregate workforce,
        OperatorAggregate operatorAgg,
        WorkforceScheduleCommand command)
    {
        if (command.ScheduleEnd <= command.ScheduleStart)
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                "ScheduleEnd must be later than ScheduleStart");

        if (!workforce.IsEligible())
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                "Worker is not active");

        if (!workforce.IsAvailable())
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                "Worker is not available");

        if (!workforce.HasCapability(command.TaskType))
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                $"Worker does not have required capability '{command.TaskType}'");

        if (!operatorAgg.IsActive())
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                "Operator is not active");

        if (!operatorAgg.HasAuthorityForScope(command.ScheduleScope))
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                $"Operator does not have authority for scope '{command.ScheduleScope}'");

        if (workforce.HasScheduleOverlap(command.ScheduleStart, command.ScheduleEnd))
            return WorkforceScheduleDecision.Rejected(workforce.WorkerId, command.TaskId,
                command.ScheduleStart, command.ScheduleEnd,
                "Schedule overlaps with an existing schedule");

        return WorkforceScheduleDecision.Success(
            workforce.WorkerId, command.TaskId,
            command.ScheduleStart, command.ScheduleEnd);
    }

    private static WorkforceScheduleCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string;
        var taskId = context.Data.GetValueOrDefault("taskId") as string;
        var taskType = context.Data.GetValueOrDefault("taskType") as string;
        var scope = context.Data.GetValueOrDefault("scheduleScope") as string;

        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;
        if (string.IsNullOrEmpty(operatorId) || !Guid.TryParse(operatorId, out var opGuid))
            return null;
        if (string.IsNullOrEmpty(taskId) || !Guid.TryParse(taskId, out var tGuid))
            return null;
        if (string.IsNullOrEmpty(taskType))
            return null;
        if (string.IsNullOrEmpty(scope))
            return null;

        var scheduleStart = ResolveDateTime(context.Data.GetValueOrDefault("scheduleStart"));
        var scheduleEnd = ResolveDateTime(context.Data.GetValueOrDefault("scheduleEnd"));

        if (scheduleStart is null || scheduleEnd is null)
            return null;

        return new WorkforceScheduleCommand(wfGuid, opGuid, tGuid, taskType,
            scheduleStart.Value, scheduleEnd.Value, scope);
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var capabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();
        var status = context.Data.GetValueOrDefault("workerStatus") as string ?? "Active";
        var availability = context.Data.GetValueOrDefault("workerAvailability") as string ?? "Available";
        var existingSchedules = context.Data.GetValueOrDefault("workerSchedules") as IEnumerable<ScheduleRecord>
            ?? Array.Empty<ScheduleRecord>();

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        var workforce = WorkforceAggregate.Register(new WorkerId(wGuid), workerName, capabilities);

        if (status == "Suspended")
            workforce.Suspend();

        if (availability == "Busy")
        {
            var currentTaskId = context.Data.GetValueOrDefault("workerCurrentTaskId") as string;
            if (Guid.TryParse(currentTaskId, out var ctGuid))
                workforce.AssignToTask(ctGuid);
        }

        foreach (var schedule in existingSchedules)
            workforce.AddSchedule(schedule);

        return workforce;
    }

    private static OperatorAggregate? ResolveOperator(EngineContext context)
    {
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string;
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
