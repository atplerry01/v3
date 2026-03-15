namespace Whycespace.CapitalSystem.Tests;

using Whycespace.System.Midstream.Capital.Evidence;

public sealed class CapitalEvidenceRecorderTests
{
    private readonly CapitalEvidenceRecorder _recorder = new();

    private static CapitalEvidenceRecord CreateRecord(
        Guid? evidenceId = null,
        CapitalEvidenceOperationType operationType = CapitalEvidenceOperationType.ContributionEvidence,
        Guid? capitalId = null,
        Guid? poolId = null,
        Guid? referenceId = null,
        decimal amount = 50_000m,
        string currency = "GBP",
        Guid? ledgerEntryId = null,
        DateTime? createdAt = null,
        Guid? traceId = null,
        Guid? correlationId = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        var capId = capitalId ?? Guid.NewGuid();
        var plId = poolId ?? Guid.NewGuid();
        var refId = referenceId ?? Guid.NewGuid();

        var hash = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            capId, plId, refId, amount, currency, timestamp);

        return new CapitalEvidenceRecord(
            EvidenceId: evidenceId ?? Guid.NewGuid(),
            OperationType: operationType,
            CapitalId: capId,
            PoolId: plId,
            ReferenceId: refId,
            Amount: amount,
            Currency: currency,
            EvidenceHash: hash,
            LedgerEntryId: ledgerEntryId ?? Guid.NewGuid(),
            CreatedAt: timestamp,
            TraceId: traceId ?? Guid.NewGuid(),
            CorrelationId: correlationId ?? Guid.NewGuid());
    }

    [Fact]
    public async Task CreateEvidenceRecord_SuccessfullyRecords()
    {
        var record = CreateRecord();

        var result = await _recorder.RecordEvidenceAsync(record);

        Assert.Equal(record.EvidenceId, result.EvidenceId);
        Assert.Equal(record.CapitalId, result.CapitalId);
        Assert.Equal(record.EvidenceHash, result.EvidenceHash);
    }

    [Fact]
    public async Task CreateEvidenceRecord_DuplicateId_Throws()
    {
        var record = CreateRecord();
        await _recorder.RecordEvidenceAsync(record);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _recorder.RecordEvidenceAsync(record));
    }

    [Fact]
    public async Task CreateEvidenceRecord_EmptyEvidenceId_Throws()
    {
        var record = CreateRecord(evidenceId: Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _recorder.RecordEvidenceAsync(record));
    }

    [Fact]
    public async Task CreateEvidenceRecord_EmptyCapitalId_Throws()
    {
        var record = CreateRecord(capitalId: Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _recorder.RecordEvidenceAsync(record));
    }

    [Fact]
    public void VerifyEvidenceHash_SameInputs_ProducesSameHash()
    {
        var capitalId = Guid.NewGuid();
        var poolId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var amount = 100_000m;
        var currency = "GBP";
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var hash1 = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            capitalId, poolId, referenceId, amount, currency, timestamp);
        var hash2 = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            capitalId, poolId, referenceId, amount, currency, timestamp);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void VerifyEvidenceHash_DifferentInputs_ProducesDifferentHash()
    {
        var capitalId = Guid.NewGuid();
        var poolId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var currency = "GBP";
        var timestamp = DateTime.UtcNow;

        var hash1 = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            capitalId, poolId, referenceId, 100_000m, currency, timestamp);
        var hash2 = CapitalEvidenceHashUtility.ComputeEvidenceHash(
            capitalId, poolId, referenceId, 200_000m, currency, timestamp);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task LedgerReferenceIntegrity_RecordHasLedgerEntryId()
    {
        var ledgerEntryId = Guid.NewGuid();
        var record = CreateRecord(ledgerEntryId: ledgerEntryId);

        var result = await _recorder.RecordEvidenceAsync(record);

        Assert.Equal(ledgerEntryId, result.LedgerEntryId);
    }

    [Fact]
    public async Task ChainAnchoringVerification_IntegrityVerified()
    {
        var record = CreateRecord();
        await _recorder.RecordEvidenceAsync(record);

        var isValid = await _recorder.VerifyEvidenceIntegrityAsync(record.EvidenceId);

        Assert.True(isValid);
    }

    [Fact]
    public async Task ChainAnchoringVerification_TamperedHash_Fails()
    {
        var original = CreateRecord();
        var tampered = original with { EvidenceHash = "tampered_hash_value" };
        await _recorder.RecordEvidenceAsync(tampered);

        var isValid = await _recorder.VerifyEvidenceIntegrityAsync(tampered.EvidenceId);

        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyEvidenceIntegrity_NonExistent_ReturnsFalse()
    {
        var isValid = await _recorder.VerifyEvidenceIntegrityAsync(Guid.NewGuid());

        Assert.False(isValid);
    }

    [Fact]
    public async Task GetEvidenceByCapitalId_ReturnsMatchingRecords()
    {
        var capitalId = Guid.NewGuid();
        var record1 = CreateRecord(capitalId: capitalId);
        var record2 = CreateRecord(capitalId: capitalId);
        var record3 = CreateRecord(); // different capital

        await _recorder.RecordEvidenceAsync(record1);
        await _recorder.RecordEvidenceAsync(record2);
        await _recorder.RecordEvidenceAsync(record3);

        var results = await _recorder.GetEvidenceByCapitalIdAsync(capitalId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(capitalId, r.CapitalId));
    }

    [Fact]
    public async Task GetEvidenceByCapitalId_NoMatch_ReturnsEmpty()
    {
        var results = await _recorder.GetEvidenceByCapitalIdAsync(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetEvidenceByReferenceId_ReturnsMatchingRecords()
    {
        var referenceId = Guid.NewGuid();
        var record1 = CreateRecord(referenceId: referenceId);
        var record2 = CreateRecord(referenceId: referenceId);

        await _recorder.RecordEvidenceAsync(record1);
        await _recorder.RecordEvidenceAsync(record2);

        var results = await _recorder.GetEvidenceByReferenceIdAsync(referenceId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(referenceId, r.ReferenceId));
    }

    [Fact]
    public async Task ConcurrentEvidenceRecording_IsThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _recorder.RecordEvidenceAsync(CreateRecord());
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task AllOperationTypes_CanBeRecorded()
    {
        foreach (var opType in Enum.GetValues<CapitalEvidenceOperationType>())
        {
            var record = CreateRecord(operationType: opType);
            var result = await _recorder.RecordEvidenceAsync(record);
            Assert.Equal(opType, result.OperationType);
        }
    }
}
