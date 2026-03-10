namespace Whycespace.Domain.Application.Commands;

using Whycespace.Shared.Commands;
using Whycespace.Shared.Location;

public sealed record RequestRideCommand(
    Guid CommandId,
    Guid UserId,
    GeoLocation PickupLocation,
    GeoLocation DropoffLocation
) : ICommand;
