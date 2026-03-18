namespace Whycespace.Engines.T2E.Economic.Vault.RateLimit.Models;

public sealed record EvaluateVaultRateLimitCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid InitiatorIdentityId,
    string OperationType,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
