namespace Whycespace.System.Downstream.Economic.Vault.Registry;

public sealed class VaultPolicyRegistry : IVaultPolicyRegistry
{
    private readonly Dictionary<Guid, VaultPolicyRegistryRecord> _policyBindings = new();
    private readonly Dictionary<Guid, List<Guid>> _vaultIndex = new();
    private readonly Dictionary<string, List<Guid>> _policyIndex = new();

    public void RegisterPolicyBinding(VaultPolicyRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required.", nameof(record));

        if (string.IsNullOrWhiteSpace(record.PolicyId))
            throw new ArgumentException("PolicyId is required.", nameof(record));

        if (_policyBindings.ContainsKey(record.PolicyBindingId))
            throw new InvalidOperationException($"Policy binding '{record.PolicyBindingId}' is already registered.");

        _policyBindings[record.PolicyBindingId] = record;

        if (!_vaultIndex.TryGetValue(record.VaultId, out var vaultList))
        {
            vaultList = new List<Guid>();
            _vaultIndex[record.VaultId] = vaultList;
        }
        vaultList.Add(record.PolicyBindingId);

        if (!_policyIndex.TryGetValue(record.PolicyId, out var policyList))
        {
            policyList = new List<Guid>();
            _policyIndex[record.PolicyId] = policyList;
        }
        policyList.Add(record.PolicyBindingId);
    }

    public VaultPolicyRegistryRecord? GetPolicyBinding(Guid policyBindingId)
    {
        _policyBindings.TryGetValue(policyBindingId, out var record);
        return record;
    }

    public IReadOnlyList<VaultPolicyRegistryRecord> GetPoliciesByVault(Guid vaultId)
    {
        if (!_vaultIndex.TryGetValue(vaultId, out var ids))
            return [];

        return ids.Select(id => _policyBindings[id]).ToList();
    }

    public IReadOnlyList<VaultPolicyRegistryRecord> GetVaultsByPolicy(string policyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyId);

        if (!_policyIndex.TryGetValue(policyId, out var ids))
            return [];

        return ids.Select(id => _policyBindings[id]).ToList();
    }

    public IReadOnlyList<VaultPolicyRegistryRecord> ListPolicyBindings() => _policyBindings.Values.ToList();
}
