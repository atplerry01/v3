namespace Whycespace.Systems.Downstream.Cwg.Vaults.Registry;

public interface IVaultRegistry
{
    void RegisterVault(VaultRegistryRecord record);
    VaultRegistryRecord? GetVault(Guid vaultId);
    IReadOnlyList<VaultRegistryRecord> GetVaultsByOwner(Guid identityId);
    IReadOnlyList<VaultRegistryRecord> GetVaultsByPurpose(string purpose);
    IReadOnlyList<VaultRegistryRecord> GetVaultsByStatus(string status);
    IReadOnlyList<VaultRegistryRecord> ListVaults();
}
