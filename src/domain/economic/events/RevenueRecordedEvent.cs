namespace Whycespace.Domain.Economic.Events;

public sealed record RevenueRecordedEvent(
    Guid RevenueId,
    Guid SpvId,
    decimal Amount,
    DateTimeOffset Timestamp
)
{
    public static RevenueRecordedEvent Create(Guid spvId, decimal amount)
        => new(Guid.NewGuid(), spvId, amount, DateTimeOffset.UtcNow);
}
