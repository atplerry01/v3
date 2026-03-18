namespace Whycespace.Engines.T2E.Economic.Vault.Distribution.Models;

public sealed record VaultProfitDistributionResult(
    Guid DistributionId,
    Guid VaultId,
    decimal TotalProfitAmount,
    int ParticipantCount,
    string Currency,
    string DistributionStatus,
    DateTime CompletedAt);