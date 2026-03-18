namespace Whycespace.Systems.Downstream.Coordination.Evidence;

public sealed record EvidenceRecord(
    Guid EvidenceId,
    string OperationType,
    string SubjectId,
    string ResourceId,
    string Action,
    string Outcome,
    string CorrelationId,
    DateTimeOffset RecordedAt,
    IReadOnlyDictionary<string, string>? Metadata = null
);
