namespace Whycespace.System.Midstream.Capital.Registry;

public sealed class CapitalRegistry : ICapitalRegistry
{
    private readonly Dictionary<Guid, CapitalRecord> _records = new();
    private readonly Dictionary<Guid, List<Guid>> _poolIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _ownerIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _spvIndex = new();
    private readonly object _lock = new();

    public void RegisterCapital(CapitalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.OwnerIdentityId == Guid.Empty)
            throw new ArgumentException("OwnerIdentityId is required.", nameof(record));

        lock (_lock)
        {
            if (_records.ContainsKey(record.CapitalId))
                throw new InvalidOperationException($"Capital '{record.CapitalId}' is already registered.");

            _records[record.CapitalId] = record;

            AddToIndex(_poolIndex, record.PoolId, record.CapitalId);
            AddToIndex(_ownerIndex, record.OwnerIdentityId, record.CapitalId);
            AddToIndex(_spvIndex, record.SPVId, record.CapitalId);
        }
    }

    public CapitalRecord? GetCapital(Guid capitalId)
    {
        lock (_lock)
        {
            _records.TryGetValue(capitalId, out var record);
            return record;
        }
    }

    public IReadOnlyList<CapitalRecord> ListCapitalByPool(Guid poolId)
    {
        lock (_lock)
        {
            if (!_poolIndex.TryGetValue(poolId, out var ids))
                return [];

            return ids.Select(id => _records[id]).ToList();
        }
    }

    public IReadOnlyList<CapitalRecord> ListCapitalByOwner(Guid ownerIdentityId)
    {
        lock (_lock)
        {
            if (!_ownerIndex.TryGetValue(ownerIdentityId, out var ids))
                return [];

            return ids.Select(id => _records[id]).ToList();
        }
    }

    public IReadOnlyList<CapitalRecord> ListCapitalBySPV(Guid spvId)
    {
        lock (_lock)
        {
            if (!_spvIndex.TryGetValue(spvId, out var ids))
                return [];

            return ids.Select(id => _records[id]).ToList();
        }
    }

    public void UpdateCapitalStatus(Guid capitalId, CapitalStatus newStatus)
    {
        lock (_lock)
        {
            if (!_records.TryGetValue(capitalId, out var existing))
                throw new InvalidOperationException($"Capital '{capitalId}' not found.");

            _records[capitalId] = existing with
            {
                Status = newStatus,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private static void AddToIndex(Dictionary<Guid, List<Guid>> index, Guid key, Guid capitalId)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<Guid>();
            index[key] = list;
        }
        list.Add(capitalId);
    }
}
