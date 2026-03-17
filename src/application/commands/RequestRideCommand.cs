namespace Whycespace.Application.Commands;

using Whycespace.Contracts.Commands;
using Whycespace.Shared.Location;

public sealed record RequestRideCommand(
    Guid CommandId,
    Guid UserId,
    GeoLocation PickupLocation,
    GeoLocation DropoffLocation
) : ICommand
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
