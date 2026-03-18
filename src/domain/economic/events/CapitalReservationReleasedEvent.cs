namespace Whycespace.Domain.Economic.Events;

public sealed record CapitalReservationReleasedEvent(
    Guid EventId,
    Guid ReservationId,
    string Reason,
    DateTimeOffset Timestamp
)
{
    public static CapitalReservationReleasedEvent Create(Guid reservationId, string reason)
        => new(Guid.NewGuid(), reservationId, reason, DateTimeOffset.UtcNow);
}
