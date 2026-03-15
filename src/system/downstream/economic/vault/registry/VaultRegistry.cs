namespace Whycespace.System.Downstream.Economic.Vault.Registry;

public sealed class VaultRegistry : IVaultRegistry
{
    private readonly Dictionary<Guid, VaultRegistryRecord> _vaults = new();
    private readonly Dictionary<Guid, List<Guid>> _ownerIndex = new();
    private readonly Dictionary<string, List<Guid>> _purposeIndex = new();
    private readonly Dictionary<string, List<Guid>> _statusIndex = new();

    public void RegisterVault(VaultRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.OwnerIdentityId == Guid.Empty)
            throw new ArgumentException("OwnerIdentityId is required.", nameof(record));

        if (_vaults.ContainsKey(record.VaultId))
            throw new InvalidOperationException($"Vault '{record.VaultId}' is already registered.");

        _vaults[record.VaultId] = record;

        if (!_ownerIndex.TryGetValue(record.OwnerIdentityId, out var ownerList))
        {
            ownerList = new List<Guid>();
            _ownerIndex[record.OwnerIdentityId] = ownerList;
        }
        ownerList.Add(record.VaultId);

        if (!_purposeIndex.TryGetValue(record.VaultPurpose, out var purposeList))
        {
            purposeList = new List<Guid>();
            _purposeIndex[record.VaultPurpose] = purposeList;
        }
        purposeList.Add(record.VaultId);

        if (!_statusIndex.TryGetValue(record.VaultStatus, out var statusList))
        {
            statusList = new List<Guid>();
            _statusIndex[record.VaultStatus] = statusList;
        }
        statusList.Add(record.VaultId);
    }

    public VaultRegistryRecord? GetVault(Guid vaultId)
    {
        _vaults.TryGetValue(vaultId, out var record);
        return record;
    }

    public IReadOnlyList<VaultRegistryRecord> GetVaultsByOwner(Guid identityId)
    {
        if (!_ownerIndex.TryGetValue(identityId, out var ids))
            return [];

        return ids.Select(id => _vaults[id]).ToList();
    }

    public IReadOnlyList<VaultRegistryRecord> GetVaultsByPurpose(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        if (!_purposeIndex.TryGetValue(purpose, out var ids))
            return [];

        return ids.Select(id => _vaults[id]).ToList();
    }

    public IReadOnlyList<VaultRegistryRecord> GetVaultsByStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (!_statusIndex.TryGetValue(status, out var ids))
            return [];

        return ids.Select(id => _vaults[id]).ToList();
    }

    public IReadOnlyList<VaultRegistryRecord> ListVaults() => _vaults.Values.ToList();
}
