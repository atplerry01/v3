namespace Whycespace.Engines.T2E.Core.Vault.Models;

public sealed record ExecuteProfitDistributionCommand(
    Guid DistributionId,
    Guid VaultId,
    Guid VaultAccountId,
    decimal TotalProfitAmount,
    string Currency,
    Guid InitiatorIdentityId,
    string DistributionReference,
    DateTime CreatedAt,
    string? Description = null);