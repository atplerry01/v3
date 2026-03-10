namespace Whycespace.EconomicDomain.Events;

public sealed record CapitalContributionRecordedEvent(
    Guid ContributionId,
    Guid SpvId,
    decimal Amount,
    DateTimeOffset Timestamp
)
{
    public static CapitalContributionRecordedEvent Create(Guid spvId, decimal amount)
        => new(Guid.NewGuid(), spvId, amount, DateTimeOffset.UtcNow);
}
