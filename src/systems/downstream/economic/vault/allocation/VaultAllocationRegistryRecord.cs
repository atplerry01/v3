namespace Whycespace.Systems.Downstream.Economic.Vault.Allocation;

public sealed record VaultAllocationRegistryRecord(
    Guid AllocationId,
    Guid VaultId,
    Guid RecipientIdentityId,
    string AllocationType,
    string AllocationStatus,
    decimal AllocationPercentage,
    decimal AllocationAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? AllocationReference = null,
    string? Description = null
);
