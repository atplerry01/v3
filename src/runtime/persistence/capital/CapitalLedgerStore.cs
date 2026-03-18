namespace Whycespace.Infrastructure.Persistence.CapitalLedger;

public sealed class CapitalLedgerStore : ICapitalLedgerStore
{
    private readonly List<CapitalLedgerEntry> _entries = new();
    private readonly Dictionary<Guid, List<int>> _capitalIndex = new();
    private readonly Dictionary<Guid, List<int>> _poolIndex = new();
    private readonly Dictionary<Guid, List<int>> _investorIndex = new();
    private readonly Dictionary<Guid, List<int>> _referenceIndex = new();
    private readonly object _lock = new();

    public void AppendEntry(CapitalLedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.EntryId == Guid.Empty)
            throw new ArgumentException("EntryId is required.", nameof(entry));

        lock (_lock)
        {
            var position = _entries.Count;
            _entries.Add(entry);

            AddToIndex(_capitalIndex, entry.CapitalId, position);
            AddToIndex(_poolIndex, entry.PoolId, position);
            AddToIndex(_investorIndex, entry.InvestorIdentityId, position);
            AddToIndex(_referenceIndex, entry.ReferenceId, position);
        }
    }

    public IReadOnlyList<CapitalLedgerEntry> GetEntriesByCapitalId(Guid capitalId)
    {
        lock (_lock)
        {
            return GetByIndex(_capitalIndex, capitalId);
        }
    }

    public IReadOnlyList<CapitalLedgerEntry> GetEntriesByPoolId(Guid poolId)
    {
        lock (_lock)
        {
            return GetByIndex(_poolIndex, poolId);
        }
    }

    public IReadOnlyList<CapitalLedgerEntry> GetEntriesByInvestor(Guid investorIdentityId)
    {
        lock (_lock)
        {
            return GetByIndex(_investorIndex, investorIdentityId);
        }
    }

    public IReadOnlyList<CapitalLedgerEntry> GetEntriesByReferenceId(Guid referenceId)
    {
        lock (_lock)
        {
            return GetByIndex(_referenceIndex, referenceId);
        }
    }

    public IReadOnlyList<CapitalLedgerEntry> GetLedgerRange(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        lock (_lock)
        {
            return _entries
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .ToList();
        }
    }

    private IReadOnlyList<CapitalLedgerEntry> GetByIndex(Dictionary<Guid, List<int>> index, Guid key)
    {
        if (!index.TryGetValue(key, out var positions))
            return [];

        return positions.Select(pos => _entries[pos]).ToList();
    }

    private static void AddToIndex(Dictionary<Guid, List<int>> index, Guid key, int position)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<int>();
            index[key] = list;
        }
        list.Add(position);
    }
}
