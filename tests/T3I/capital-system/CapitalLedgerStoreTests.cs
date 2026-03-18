namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Systems.Midstream.Capital.Stores;

public sealed class CapitalLedgerStoreTests
{
    private readonly CapitalLedgerStore _store = new();

    private static CapitalLedgerEntry CreateEntry(
        Guid? entryId = null,
        LedgerEntryType entryType = LedgerEntryType.ContributionRecorded,
        Guid? capitalId = null,
        Guid? poolId = null,
        Guid? investorIdentityId = null,
        Guid? referenceId = null,
        decimal amount = 1000m,
        string currency = "GBP",
        decimal previousBalance = 0m,
        decimal? newBalance = null,
        DateTimeOffset? timestamp = null,
        string? traceId = null,
        string? correlationId = null)
    {
        return new CapitalLedgerEntry(
            EntryId: entryId ?? Guid.NewGuid(),
            EntryType: entryType,
            CapitalId: capitalId ?? Guid.NewGuid(),
            PoolId: poolId ?? Guid.NewGuid(),
            InvestorIdentityId: investorIdentityId ?? Guid.NewGuid(),
            ReferenceId: referenceId ?? Guid.NewGuid(),
            Amount: amount,
            Currency: currency,
            PreviousBalance: previousBalance,
            NewBalance: newBalance ?? previousBalance + amount,
            Timestamp: timestamp ?? DateTimeOffset.UtcNow,
            TraceId: traceId ?? Guid.NewGuid().ToString(),
            CorrelationId: correlationId ?? Guid.NewGuid().ToString());
    }

    [Fact]
    public void AppendEntry_SuccessfullyAppends()
    {
        var entry = CreateEntry();

        _store.AppendEntry(entry);

        var results = _store.GetEntriesByCapitalId(entry.CapitalId);
        Assert.Single(results);
        Assert.Equal(entry.EntryId, results[0].EntryId);
        Assert.Equal(entry.Amount, results[0].Amount);
    }

    [Fact]
    public void AppendEntry_NullEntry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _store.AppendEntry(null!));
    }

    [Fact]
    public void AppendEntry_EmptyEntryId_Throws()
    {
        var entry = CreateEntry(entryId: Guid.Empty);

        Assert.Throws<ArgumentException>(() => _store.AppendEntry(entry));
    }

    [Fact]
    public void GetEntriesByCapitalId_ReturnsMatchingEntries()
    {
        var capitalId = Guid.NewGuid();
        var entry1 = CreateEntry(capitalId: capitalId);
        var entry2 = CreateEntry(capitalId: capitalId);
        var entry3 = CreateEntry(); // different capital

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetEntriesByCapitalId(capitalId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(capitalId, r.CapitalId));
    }

    [Fact]
    public void GetEntriesByCapitalId_NoMatch_ReturnsEmpty()
    {
        var results = _store.GetEntriesByCapitalId(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetEntriesByPoolId_ReturnsMatchingEntries()
    {
        var poolId = Guid.NewGuid();
        var entry1 = CreateEntry(poolId: poolId);
        var entry2 = CreateEntry(poolId: poolId);
        var entry3 = CreateEntry(); // different pool

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetEntriesByPoolId(poolId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(poolId, r.PoolId));
    }

    [Fact]
    public void GetEntriesByPoolId_NoMatch_ReturnsEmpty()
    {
        var results = _store.GetEntriesByPoolId(Guid.NewGuid());
        Assert.Empty(results);
    }

    [Fact]
    public void GetEntriesByInvestor_ReturnsMatchingEntries()
    {
        var investorId = Guid.NewGuid();
        var entry1 = CreateEntry(investorIdentityId: investorId);
        var entry2 = CreateEntry(investorIdentityId: investorId);
        var entry3 = CreateEntry(); // different investor

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetEntriesByInvestor(investorId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(investorId, r.InvestorIdentityId));
    }

    [Fact]
    public void GetEntriesByReferenceId_ReturnsMatchingEntries()
    {
        var referenceId = Guid.NewGuid();
        var entry1 = CreateEntry(referenceId: referenceId);
        var entry2 = CreateEntry(referenceId: referenceId);
        var entry3 = CreateEntry(); // different reference

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetEntriesByReferenceId(referenceId);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(referenceId, r.ReferenceId));
    }

    [Fact]
    public void GetLedgerRange_ReturnsEntriesWithinRange()
    {
        var now = DateTimeOffset.UtcNow;
        var entry1 = CreateEntry(timestamp: now.AddHours(-2));
        var entry2 = CreateEntry(timestamp: now.AddHours(-1));
        var entry3 = CreateEntry(timestamp: now.AddHours(1)); // outside range

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetLedgerRange(now.AddHours(-3), now);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void GetLedgerRange_NoMatch_ReturnsEmpty()
    {
        var entry = CreateEntry(timestamp: DateTimeOffset.UtcNow);
        _store.AppendEntry(entry);

        var results = _store.GetLedgerRange(
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(-5));

        Assert.Empty(results);
    }

    [Fact]
    public void LedgerImmutability_RecordIsUnchangedAfterAppend()
    {
        var entry = CreateEntry(amount: 5000m, previousBalance: 0m, newBalance: 5000m);
        _store.AppendEntry(entry);

        var results = _store.GetEntriesByCapitalId(entry.CapitalId);

        Assert.Single(results);
        Assert.Equal(5000m, results[0].Amount);
        Assert.Equal(0m, results[0].PreviousBalance);
        Assert.Equal(5000m, results[0].NewBalance);
        Assert.Equal(entry.TraceId, results[0].TraceId);
        Assert.Equal(entry.CorrelationId, results[0].CorrelationId);
    }

    [Fact]
    public void AppendEntry_PreservesInsertionOrder()
    {
        var capitalId = Guid.NewGuid();
        var entry1 = CreateEntry(capitalId: capitalId, amount: 100m);
        var entry2 = CreateEntry(capitalId: capitalId, amount: 200m);
        var entry3 = CreateEntry(capitalId: capitalId, amount: 300m);

        _store.AppendEntry(entry1);
        _store.AppendEntry(entry2);
        _store.AppendEntry(entry3);

        var results = _store.GetEntriesByCapitalId(capitalId);

        Assert.Equal(3, results.Count);
        Assert.Equal(100m, results[0].Amount);
        Assert.Equal(200m, results[1].Amount);
        Assert.Equal(300m, results[2].Amount);
    }

    [Fact]
    public async Task ConcurrentLedgerWrites_AreThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _store.AppendEntry(CreateEntry());
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
    public void AllEntryTypes_AreRecorded()
    {
        var capitalId = Guid.NewGuid();
        var types = new[]
        {
            LedgerEntryType.CommitmentRecorded,
            LedgerEntryType.ContributionRecorded,
            LedgerEntryType.ReservationRecorded,
            LedgerEntryType.AllocationRecorded,
            LedgerEntryType.UtilizationRecorded,
            LedgerEntryType.DistributionRecorded,
            LedgerEntryType.DistributionReversal
        };

        foreach (var type in types)
        {
            _store.AppendEntry(CreateEntry(capitalId: capitalId, entryType: type));
        }

        var results = _store.GetEntriesByCapitalId(capitalId);

        Assert.Equal(7, results.Count);
        Assert.Equal(types.Length, results.Select(r => r.EntryType).Distinct().Count());
    }
}
