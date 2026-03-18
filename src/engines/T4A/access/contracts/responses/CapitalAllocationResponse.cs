namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record CapitalAllocationResponse(
    string AllocationId,
    string VaultId,
    string SpvId,
    decimal Amount,
    string Currency,
    string Status);
