namespace Whycespace.Engines.T3I.Reporting.Workforce;

using System.Security.Cryptography;
using System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkforceAudit", EngineTier.T3I, EngineKind.Projection, "WorkforceAuditCommand", typeof(EngineEvent))]
public sealed class WorkforceAuditEngine : IEngine
{
    public string Name => "WorkforceAudit";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid audit command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var record = GenerateAuditRecord(workforce, command);

        var events = new[]
        {
            EngineEvent.Create("WorkforceAuditRecordCreated", command.WorkforceId,
                new Dictionary<string, object>
                {
                    ["auditId"] = record.AuditId.ToString(),
                    ["workforceId"] = record.WorkforceId.ToString(),
                    ["actionType"] = record.ActionType.ToString(),
                    ["actionReferenceId"] = record.ActionReferenceId.ToString(),
                    ["performedBy"] = record.PerformedBy.ToString(),
                    ["timestamp"] = record.Timestamp.ToString("O"),
                    ["auditSummary"] = record.AuditSummary,
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["auditId"] = record.AuditId.ToString(),
                ["workforceId"] = record.WorkforceId.ToString(),
                ["actionType"] = record.ActionType.ToString(),
                ["auditSummary"] = record.AuditSummary
            }));
    }

    public static WorkforceAuditRecord GenerateAuditRecord(
        WorkforceAggregate workforce,
        WorkforceAuditCommand command)
    {
        var auditId = GenerateDeterministicAuditId(
            command.WorkforceId, command.ActionReferenceId, command.Timestamp);

        var summary = BuildAuditSummary(workforce, command);

        return new WorkforceAuditRecord(
            auditId,
            command.WorkforceId,
            command.ActionType,
            command.ActionReferenceId,
            command.PerformedBy,
            command.Timestamp,
            summary);
    }

    private static Guid GenerateDeterministicAuditId(
        Guid workforceId, Guid actionReferenceId, DateTimeOffset timestamp)
    {
        var input = $"{workforceId}:{actionReferenceId}:{timestamp:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }

    private static string BuildAuditSummary(
        WorkforceAggregate workforce, WorkforceAuditCommand command)
    {
        var workerName = workforce.Name;
        return command.ActionType switch
        {
            AuditActionType.WorkforceRegistered =>
                $"Worker '{workerName}' registered. {command.Details}",
            AuditActionType.CapabilityAssigned =>
                $"Capability assigned to worker '{workerName}'. {command.Details}",
            AuditActionType.AvailabilityChanged =>
                $"Availability changed for worker '{workerName}'. {command.Details}",
            AuditActionType.AssignmentCreated =>
                $"Assignment created for worker '{workerName}'. {command.Details}",
            AuditActionType.ScheduleCreated =>
                $"Schedule created for worker '{workerName}'. {command.Details}",
            AuditActionType.LifecycleChanged =>
                $"Lifecycle changed for worker '{workerName}'. {command.Details}",
            AuditActionType.IncentiveEvaluated =>
                $"Incentive evaluated for worker '{workerName}'. {command.Details}",
            _ => $"Unknown action for worker '{workerName}'. {command.Details}"
        };
    }

    private static WorkforceAuditCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        var actionType = context.Data.GetValueOrDefault("actionType") as string;
        var actionReferenceId = context.Data.GetValueOrDefault("actionReferenceId") as string;
        var performedBy = context.Data.GetValueOrDefault("performedBy") as string;
        var timestamp = ResolveDateTime(context.Data.GetValueOrDefault("timestamp"));
        var details = context.Data.GetValueOrDefault("details") as string;

        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;
        if (string.IsNullOrEmpty(actionType) || !Enum.TryParse<AuditActionType>(actionType, true, out var action))
            return null;
        if (string.IsNullOrEmpty(actionReferenceId) || !Guid.TryParse(actionReferenceId, out var refGuid))
            return null;
        if (string.IsNullOrEmpty(performedBy) || !Guid.TryParse(performedBy, out var byGuid))
            return null;
        if (timestamp is null)
            return null;

        return new WorkforceAuditCommand(wfGuid, action, refGuid, byGuid, timestamp.Value, details ?? "");
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var workerCapabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        return WorkforceAggregate.Register(new WorkerId(wGuid), workerName, workerCapabilities);
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
