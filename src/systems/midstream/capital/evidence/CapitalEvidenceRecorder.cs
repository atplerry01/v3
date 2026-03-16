namespace Whycespace.Systems.Midstream.Capital.Evidence;

using global::System.Collections.Concurrent;

public sealed class CapitalEvidenceRecorder : ICapitalEvidenceRecorder
{
    private readonly ConcurrentDictionary<Guid, CapitalEvidenceRecord> _evidenceStore = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _capitalIndex = new();
    private readonly ConcurrentDictionary<Guid, List<Guid>> _referenceIndex = new();
    private readonly object _indexLock = new();

    public Task<CapitalEvidenceRecord> RecordEvidenceAsync(CapitalEvidenceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.EvidenceId == Guid.Empty)
            throw new ArgumentException("EvidenceId must not be empty.");

        if (record.CapitalId == Guid.Empty)
            throw new ArgumentException("CapitalId must not be empty.");

        if (record.PoolId == Guid.Empty)
            throw new ArgumentException("PoolId must not be empty.");

        if (string.IsNullOrWhiteSpace(record.Currency))
            throw new ArgumentException("Currency must not be empty.");

        if (string.IsNullOrWhiteSpace(record.EvidenceHash))
            throw new ArgumentException("EvidenceHash must not be empty.");

        if (!_evidenceStore.TryAdd(record.EvidenceId, record))
            throw new InvalidOperationException($"Evidence record with id {record.EvidenceId} already exists.");

        lock (_indexLock)
        {
            _capitalIndex.GetOrAdd(record.CapitalId, _ => []).Add(record.EvidenceId);
            _referenceIndex.GetOrAdd(record.ReferenceId, _ => []).Add(record.EvidenceId);
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<CapitalEvidenceRecord>> GetEvidenceByCapitalIdAsync(Guid capitalId)
    {
        if (!_capitalIndex.TryGetValue(capitalId, out var evidenceIds))
            return Task.FromResult<IReadOnlyList<CapitalEvidenceRecord>>([]);

        List<CapitalEvidenceRecord> results;
        lock (_indexLock)
        {
            results = evidenceIds
                .Where(id => _evidenceStore.ContainsKey(id))
                .Select(id => _evidenceStore[id])
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<CapitalEvidenceRecord>>(results);
    }

    public Task<IReadOnlyList<CapitalEvidenceRecord>> GetEvidenceByReferenceIdAsync(Guid referenceId)
    {
        if (!_referenceIndex.TryGetValue(referenceId, out var evidenceIds))
            return Task.FromResult<IReadOnlyList<CapitalEvidenceRecord>>([]);

        List<CapitalEvidenceRecord> results;
        lock (_indexLock)
        {
            results = evidenceIds
                .Where(id => _evidenceStore.ContainsKey(id))
                .Select(id => _evidenceStore[id])
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<CapitalEvidenceRecord>>(results);
    }

    public Task<bool> VerifyEvidenceIntegrityAsync(Guid evidenceId)
    {
        if (!_evidenceStore.TryGetValue(evidenceId, out var record))
            return Task.FromResult(false);

        var recomputedHash = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            record.CapitalId,
            record.PoolId,
            record.ReferenceId,
            record.Amount,
            record.Currency,
            record.CreatedAt);

        return Task.FromResult(string.Equals(record.EvidenceHash, recomputedHash, StringComparison.Ordinal));
    }
}
