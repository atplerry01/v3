namespace Whycespace.Systems.Downstream.Cwg.Vaults.Allocation;

public interface IVaultAllocationRegistry
{
    void RegisterAllocation(VaultAllocationRegistryRecord record);
    VaultAllocationRegistryRecord? GetAllocation(Guid allocationId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByVault(Guid vaultId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByRecipient(Guid identityId);
    IReadOnlyList<VaultAllocationRegistryRecord> GetAllocationsByType(string allocationType);
    IReadOnlyList<VaultAllocationRegistryRecord> ListAllocations();
}
