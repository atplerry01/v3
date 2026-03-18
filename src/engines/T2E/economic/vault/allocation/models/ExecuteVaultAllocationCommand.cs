namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ExecuteVaultAllocationCommand(
    Guid AllocationId,
    Guid VaultId,
    Guid RecipientIdentityId,
    string AllocationType,
    decimal AllocationPercentage,
    decimal AllocationAmount,
    Guid InitiatorIdentityId,
    DateTime CreatedAt,
    string? Description = null,
    string? AllocationReference = null);
