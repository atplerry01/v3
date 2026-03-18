namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GuardianRecordStore : IGuardianRegistryStore
{
    private readonly ConcurrentDictionary<Guid, GuardianRecord> _records = new();
    private readonly ConcurrentDictionary<string, Guid> _identityIndex = new();

    public void Save(GuardianRecord record)
    {
        if (!_records.TryAdd(record.GuardianId, record))
            throw new InvalidOperationException($"Guardian already exists: {record.GuardianId}");

        if (!_identityIndex.TryAdd(record.IdentityId, record.GuardianId))
        {
            _records.TryRemove(record.GuardianId, out _);
            throw new InvalidOperationException($"Identity already registered: {record.IdentityId}");
        }
    }

    public GuardianRecord? GetById(Guid guardianId)
    {
        _records.TryGetValue(guardianId, out var record);
        return record;
    }

    public GuardianRecord? GetByIdentity(string identityId)
    {
        if (_identityIndex.TryGetValue(identityId, out var guardianId))
            return GetById(guardianId);

        return null;
    }

    public IReadOnlyList<GuardianRecord> GetAll()
    {
        return _records.Values.ToList();
    }

    public IReadOnlyList<GuardianRecord> GetByRole(GuardianRole role)
    {
        return _records.Values
            .Where(r => r.GuardianRole == role)
            .ToList();
    }

    public IReadOnlyList<GuardianRecord> GetByDomain(string domain)
    {
        return _records.Values
            .Where(r => r.AuthorityDomains.Contains(domain))
            .ToList();
    }

    public void Update(GuardianRecord record)
    {
        if (!_records.ContainsKey(record.GuardianId))
            throw new KeyNotFoundException($"Guardian not found: {record.GuardianId}");

        _records[record.GuardianId] = record;
    }

    public bool ExistsById(Guid guardianId)
    {
        return _records.ContainsKey(guardianId);
    }

    public bool ExistsByIdentity(string identityId)
    {
        return _identityIndex.ContainsKey(identityId);
    }
}
