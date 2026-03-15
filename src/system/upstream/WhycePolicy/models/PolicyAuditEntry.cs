namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyAuditEntry(
    string AuditId,
    string PolicyId,
    string ActionType,
    string ActorId,
    DateTime Timestamp,
    string? EvidenceId,
    string Summary
);
