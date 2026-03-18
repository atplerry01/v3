namespace Whycespace.Systems.Downstream.Coordination.Evidence;

public sealed class EvidenceRecorder
{
    private readonly List<EvidenceRecord> _records = new();

    public EvidenceRecord Record(string operationType, string subjectId, string resourceId, string action, string outcome, string correlationId, IReadOnlyDictionary<string, string>? metadata = null)
    {
        var record = new EvidenceRecord(
            Guid.NewGuid(),
            operationType,
            subjectId,
            resourceId,
            action,
            outcome,
            correlationId,
            DateTimeOffset.UtcNow,
            metadata
        );

        _records.Add(record);
        return record;
    }

    public IReadOnlyList<EvidenceRecord> GetByCorrelation(string correlationId)
        => _records.Where(r => r.CorrelationId == correlationId).ToList();

    public IReadOnlyList<EvidenceRecord> GetBySubject(string subjectId)
        => _records.Where(r => r.SubjectId == subjectId).ToList();

    public IReadOnlyList<EvidenceRecord> GetByOperation(string operationType)
        => _records.Where(r => r.OperationType == operationType).ToList();

    public IReadOnlyList<EvidenceRecord> ListAll() => _records.ToList();
}
