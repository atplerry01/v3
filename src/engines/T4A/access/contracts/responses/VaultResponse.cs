namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record VaultResponse(
    string VaultId,
    string Name,
    string SpvId,
    string Currency,
    string Status);
