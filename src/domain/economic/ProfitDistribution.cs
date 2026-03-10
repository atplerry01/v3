namespace Whycespace.Domain.Economic;

public sealed record ProfitDistribution(
    Guid DistributionId,
    Guid SpvId,
    Guid VaultId,
    decimal Amount,
    DateTimeOffset DistributedAt
);
