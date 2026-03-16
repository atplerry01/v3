namespace Whycespace.Systems.Downstream.Economic.Vault.Allocation;

public interface IVaultAllocationRegistry
{
    void RegisterAllocation(VaultAllocationRegistryRecord record);
    VaultAllocationRegistryRecord? GetAllocation(Guid allocationId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByVault(Guid vaultId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByRecipient(Guid identityId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByType(string allocationType);
    IReadOnlyList<VaultAllocationRegistryRecord> ListAllocations();
}
