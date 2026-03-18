namespace Whycespace.Systems.Downstream.Cwg.Vaults.Registry;

public sealed record VaultRegistryRecord(
    Guid VaultId,
    string VaultName,
    Guid OwnerIdentityId,
    string VaultPurpose,
    string VaultStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Cluster = null,
    string? SubCluster = null,
    string? SPV = null
);
