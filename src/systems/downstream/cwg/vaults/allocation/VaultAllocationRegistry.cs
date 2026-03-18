namespace Whycespace.Systems.Downstream.Cwg.Vaults.Allocation;

public sealed class VaultAllocationRegistry : IVaultAllocationRegistry
{
    private readonly Dictionary<Guid, VaultAllocationRegistryRecord> _allocations = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();
    private readonly Dictionary<Guid, List<Guid>> _recipientIndex = new();
    private readonly Dictionary<string, List<Guid>> _typeIndex = new();

    public void RegisterAllocation(VaultAllocationRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required.", nameof(record));

        if (record.RecipientIdentityId == Guid.Empty)
            throw new ArgumentException("RecipientIdentityId is required.", nameof(record));

        ArgumentException.ThrowIfNullOrWhiteSpace(record.AllocationType, nameof(record.AllocationType));

        if (_allocations.ContainsKey(record.AllocationId))
            throw new InvalidOperationException($"Allocation '{record.AllocationId}' is already registered.");

        _allocations[record.AllocationId] = record;

        if (!_vaultIndex.TryGetValue(record.VaultId, out var vaultList))
        {
            vaultList = new List<Guid>();
            _vaultIndex[record.VaultId] = vaultList;
        }
        vaultList.Add(record.AllocationId);

        if (!_recipientIndex.TryGetValue(record.RecipientIdentityId, out var recipientList))
        {
            recipientList = new List<Guid>();
            _recipientIndex[record.RecipientIdentityId] = recipientList;
        }
        recipientList.Add(record.AllocationId);

        if (!_typeIndex.TryGetValue(record.AllocationType, out var typeList))
        {
            typeList = new List<Guid>();
            _typeIndex[record.AllocationType] = typeList;
        }
        typeList.Add(record.AllocationId);
    }

    public VaultAllocationRegistryRecord? GetAllocation(Guid allocationId)
    {
        _allocations.TryGetValue(allocationId, out var record);
        return record;
    }

    public IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _allocations[id]).ToList();
    }

    public IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByRecipient(Guid identityId)
    {
        if (!_recipientIndex.TryGetValue(identityId, out var ids))
            return [];

        return ids.Select(id => _allocations[id]).ToList();
    }

    public IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByType(string allocationType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(allocationType);

        if (!_typeIndex.TryGetValue(allocationType, out var ids))
            return [];

        return ids.Select(id => _allocations[id]).ToList();
    }

    public IReadOnlyList<VaultAllocationRegistryRecord> ListAllocations() => _allocations.Values.ToList();
}
