namespace Whycespace.Domain.Core.Economic.Events;

public sealed record CapitalReservationExpiredEvent(
    Guid EventId,
    Guid ReservationId,
    string ExpirationReason,
    DateTimeOffset Timestamp
)
{
    public static CapitalReservationExpiredEvent Create(Guid reservationId, string expirationReason)
        => new(Guid.NewGuid(), reservationId, expirationReason, DateTimeOffset.UtcNow);
}
