namespace Whycespace.Domain.Events.Core.Economic;

public sealed record CapitalReservedEvent(
    Guid EventId,
    Guid ReservationId,
    Guid PoolId,
    string TargetType,
    Guid TargetId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp
)
{
    public static CapitalReservedEvent Create(
        Guid reservationId, Guid poolId, string targetType, Guid targetId, decimal amount, string currency)
        => new(Guid.NewGuid(), reservationId, poolId, targetType, targetId, amount, currency, DateTimeOffset.UtcNow);
}
