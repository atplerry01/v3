namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyEvidenceStore
{
    private readonly ConcurrentDictionary<string, PolicyEvidenceRecord> _records = new();

    public void RecordEvidence(PolicyEvidenceRecord record)
    {
        if (!_records.TryAdd(record.EvidenceId, record))
            throw new InvalidOperationException($"Evidence record '{record.EvidenceId}' already exists.");
    }

    public PolicyEvidenceRecord? GetEvidence(string evidenceId)
    {
        _records.TryGetValue(evidenceId, out var record);
        return record;
    }

    public IReadOnlyList<PolicyEvidenceRecord> GetAllEvidence()
    {
        return _records.Values.ToList();
    }
}
