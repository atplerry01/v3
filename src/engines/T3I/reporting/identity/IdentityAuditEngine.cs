namespace Whycespace.Engines.T3I.Reporting.Identity;

using System.Security.Cryptography;
using System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityAudit", EngineTier.T3I, EngineKind.Projection, "IdentityAuditCommand", typeof(EngineEvent))]
public sealed class IdentityAuditEngine : IEngine
{
    public string Name => "IdentityAudit";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid audit command: missing required fields"));

        var record = GenerateAuditRecord(command);

        var events = new[]
        {
            EngineEvent.Create("IdentityAuditRecorded", command.IdentityId,
                new Dictionary<string, object>
                {
                    ["auditId"] = record.AuditId.ToString(),
                    ["identityId"] = record.IdentityId.ToString(),
                    ["action"] = record.Action.ToString(),
                    ["sourceSystem"] = record.SourceSystem,
                    ["performedBy"] = record.PerformedBy.ToString(),
                    ["operationReferenceId"] = record.OperationReferenceId.ToString(),
                    ["metadata"] = record.Metadata,
                    ["recordedAt"] = record.RecordedAt.ToString("O"),
                    ["topic"] = "whyce.identity.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["auditId"] = record.AuditId.ToString(),
                ["identityId"] = record.IdentityId.ToString(),
                ["action"] = record.Action.ToString(),
                ["sourceSystem"] = record.SourceSystem,
                ["recordedAt"] = record.RecordedAt.ToString("O")
            }));
    }

    public static IdentityAuditRecord GenerateAuditRecord(IdentityAuditCommand command)
    {
        var auditId = GenerateDeterministicAuditId(
            command.IdentityId, command.AuditAction, command.Timestamp);

        return new IdentityAuditRecord(
            auditId,
            command.IdentityId,
            command.AuditAction,
            command.SourceSystem,
            command.PerformedBy,
            command.OperationReferenceId,
            command.Metadata,
            command.Timestamp);
    }

    private static Guid GenerateDeterministicAuditId(
        Guid identityId, IdentityAuditAction action, DateTimeOffset timestamp)
    {
        var input = $"{identityId}:{action}:{timestamp:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }

    private static IdentityAuditCommand? ResolveCommand(EngineContext context)
    {
        var identityId = context.Data.GetValueOrDefault("identityId") as string;
        var auditAction = context.Data.GetValueOrDefault("auditAction") as string;
        var sourceSystem = context.Data.GetValueOrDefault("sourceSystem") as string;
        var performedBy = context.Data.GetValueOrDefault("performedBy") as string;
        var operationReferenceId = context.Data.GetValueOrDefault("operationReferenceId") as string;
        var metadata = context.Data.GetValueOrDefault("metadata") as string ?? "";
        var timestamp = ResolveDateTime(context.Data.GetValueOrDefault("timestamp"));

        if (string.IsNullOrEmpty(identityId) || !Guid.TryParse(identityId, out var idGuid))
            return null;
        if (string.IsNullOrEmpty(auditAction) || !Enum.TryParse<IdentityAuditAction>(auditAction, true, out var action))
            return null;
        if (string.IsNullOrEmpty(sourceSystem))
            return null;
        if (string.IsNullOrEmpty(performedBy) || !Guid.TryParse(performedBy, out var byGuid))
            return null;
        if (string.IsNullOrEmpty(operationReferenceId) || !Guid.TryParse(operationReferenceId, out var refGuid))
            return null;
        if (timestamp is null)
            return null;

        return new IdentityAuditCommand(idGuid, action, sourceSystem, byGuid, refGuid, metadata, timestamp.Value);
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
