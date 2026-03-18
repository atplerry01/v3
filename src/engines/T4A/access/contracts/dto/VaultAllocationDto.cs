namespace Whycespace.Engines.T4A.Access.Contracts.Dto;

public sealed record VaultAllocationDto(
    string AllocationId,
    string VaultId,
    string SpvId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset AllocatedAt);
