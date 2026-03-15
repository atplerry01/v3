namespace Whycespace.System.Downstream.Economic.Vault.Registry;

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
