namespace Whycespace.Domain.Core.Economic.Events;

public sealed record CapitalDistributedEvent(
    Guid EventId,
    Guid DistributionId,
    Guid PoolId,
    string TargetType,
    Guid TargetId,
    decimal TotalAmount,
    string Currency,
    string DistributionType,
    DateTimeOffset Timestamp
)
{
    public static CapitalDistributedEvent Create(
        Guid poolId, string targetType, Guid targetId,
        decimal totalAmount, string currency, string distributionType)
        => new(Guid.NewGuid(), Guid.NewGuid(), poolId, targetType, targetId,
            totalAmount, currency, distributionType, DateTimeOffset.UtcNow);
}

public sealed record CapitalDistributionAdjustedEvent(
    Guid EventId,
    Guid DistributionId,
    decimal AdjustmentAmount,
    string Reason,
    DateTimeOffset Timestamp
)
{
    public static CapitalDistributionAdjustedEvent Create(
        Guid distributionId, decimal adjustmentAmount, string reason)
        => new(Guid.NewGuid(), distributionId, adjustmentAmount, reason, DateTimeOffset.UtcNow);
}

public sealed record CapitalDistributionReversedEvent(
    Guid EventId,
    Guid DistributionId,
    string Reason,
    DateTimeOffset Timestamp
)
{
    public static CapitalDistributionReversedEvent Create(
        Guid distributionId, string reason)
        => new(Guid.NewGuid(), distributionId, reason, DateTimeOffset.UtcNow);
}
