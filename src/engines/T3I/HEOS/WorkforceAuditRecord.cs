namespace Whycespace.Engines.T3I.HEOS;

public sealed record WorkforceAuditRecord(
    Guid AuditId,
    Guid WorkforceId,
    AuditActionType ActionType,
    Guid ActionReferenceId,
    Guid PerformedBy,
    DateTimeOffset Timestamp,
    string AuditSummary
);
