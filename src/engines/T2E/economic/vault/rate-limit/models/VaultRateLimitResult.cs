namespace Whycespace.Engines.T2E.Economic.Vault.RateLimit.Models;

public sealed record VaultRateLimitResult(
    Guid VaultId,
    string OperationType,
    int CurrentOperationCount,
    int MaxAllowedOperations,
    TimeSpan WindowDuration,
    bool IsAllowed,
    string RateLimitStatus,
    string RateLimitReason,
    DateTime EvaluatedAt);
