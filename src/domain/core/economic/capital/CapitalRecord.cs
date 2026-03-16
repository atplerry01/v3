namespace Whycespace.Domain.Core.Economic.CapitalRegistry;

public sealed record CapitalRecord(
    Guid CapitalId,
    CapitalType CapitalType,
    Guid PoolId,
    Guid OwnerIdentityId,
    Guid ClusterId,
    Guid SubClusterId,
    Guid SPVId,
    decimal Amount,
    string Currency,
    CapitalStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
