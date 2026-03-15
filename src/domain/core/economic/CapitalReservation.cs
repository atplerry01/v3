namespace Whycespace.Domain.Core.Economic;

public sealed record CapitalReservation(
    Guid ReservationId,
    Guid PoolId,
    string TargetType,
    Guid TargetId,
    decimal Amount,
    string Currency,
    string Status,
    string ReservedBy,
    DateTimeOffset ReservedAt
);
