namespace Whycespace.Engines.T3I.Reporting.Identity;

public sealed record IdentityAuditRecord(
    Guid AuditId,
    Guid IdentityId,
    IdentityAuditAction Action,
    string SourceSystem,
    Guid PerformedBy,
    Guid OperationReferenceId,
    string Metadata,
    DateTimeOffset RecordedAt
);
