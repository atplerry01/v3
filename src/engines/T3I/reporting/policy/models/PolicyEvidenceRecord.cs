namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

public sealed record PolicyEvidenceRecord(
    string EvidenceId,
    string PolicyId,
    string ActionType,
    string ActorId,
    string EvidenceHash,
    string ContextHash,
    DateTime RecordedAt
);
