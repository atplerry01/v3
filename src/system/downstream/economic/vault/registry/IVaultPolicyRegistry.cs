namespace Whycespace.System.Downstream.Economic.Vault.Registry;

public interface IVaultPolicyRegistry
{
    void RegisterPolicyBinding(VaultPolicyRegistryRecord record);
    VaultPolicyRegistryRecord? GetPolicyBinding(Guid policyBindingId);
    IReadOnlyList<VaultPolicyRegistryRecord> GetPoliciesByVault(Guid vaultId);
    IReadOnlyList<VaultPolicyRegistryRecord> GetVaultsByPolicy(string policyId);
    IReadOnlyList<VaultPolicyRegistryRecord> ListPolicyBindings();
}
