namespace Whycespace.Engines.T2E.Economic.Vault.Allocation.Models;

public sealed record VaultAllocationResult(
    Guid AllocationId,
    Guid VaultId,
    Guid RecipientIdentityId,
    string AllocationType,
    decimal AllocationPercentage,
    decimal AllocationAmount,
    string AllocationStatus,
    DateTime CompletedAt);
