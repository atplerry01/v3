namespace Whycespace.Engines.T3I.WhycePolicy;

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
