namespace Whycespace.EconomicDomain.Events;

public sealed record ProfitDistributedEvent(
    Guid DistributionId,
    Guid SpvId,
    Guid ParticipantId,
    decimal Amount,
    DateTimeOffset Timestamp
)
{
    public static ProfitDistributedEvent Create(Guid spvId, Guid participantId, decimal amount)
        => new(Guid.NewGuid(), spvId, participantId, amount, DateTimeOffset.UtcNow);
}
