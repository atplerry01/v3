namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

public sealed record PolicyAuditRecord(
    string AuditId,
    string PolicyId,
    PolicyAuditActionType ActionType,
    string Decision,
    string ActorId,
    DateTime Timestamp,
    string ContextHash,
    string Summary
);
