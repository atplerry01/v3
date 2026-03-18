using Whycespace.Engines.T3I.Reporting.Identity.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Reporting.Identity.Engines;

using System.Security.Cryptography;
using System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("IdentityAudit", EngineTier.T3I, EngineKind.Projection, "IdentityAuditCommand", typeof(EngineEvent))]
public sealed class IdentityAuditEngine : IEngine, IIntelligenceEngine<EngineContext, EngineResult>
{
    public string Name => "IdentityAudit";
    public string EngineName => "IdentityAudit";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var intelligenceContext = IntelligenceContext<EngineContext>.Create(context.InvocationId, context);
        var result = Execute(intelligenceContext);
        return Task.FromResult(result.Success ? result.Output! : EngineResult.Fail(result.Error!));
    }

    public IntelligenceResult<EngineResult> Execute(IntelligenceContext<EngineContext> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var engineContext = context.Input;

        var command = ResolveCommand(engineContext);
        if (command is null)
        {
            var failResult = EngineResult.Fail("Invalid audit command: missing required fields");
            return IntelligenceResult<EngineResult>.Fail("Invalid audit command: missing required fields", IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

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

        var engineResult = EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["auditId"] = record.AuditId.ToString(),
                ["identityId"] = record.IdentityId.ToString(),
                ["action"] = record.Action.ToString(),
                ["sourceSystem"] = record.SourceSystem,
                ["recordedAt"] = record.RecordedAt.ToString("O")
            });
        return IntelligenceResult<EngineResult>.Ok(engineResult, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
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
