namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.System.Midstream.WSS.Dispatcher;
using Whycespace.Contracts.Commands;

[ApiController]
[Route("api/commands")]
public sealed class CommandController : ControllerBase
{
    private readonly CommandDispatcher _dispatcher;

    public CommandController(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("ride/request")]
    public async Task<IActionResult> RequestRide([FromBody] RideRequestDto dto)
    {
        var command = new Domain.Application.Commands.RequestRideCommand(
            Guid.NewGuid(), dto.UserId,
            new Shared.Location.GeoLocation(dto.PickupLatitude, dto.PickupLongitude),
            new Shared.Location.GeoLocation(dto.DropoffLatitude, dto.DropoffLongitude));

        var context = new Dictionary<string, object>
        {
            ["userId"] = dto.UserId.ToString(),
            ["pickupLatitude"] = dto.PickupLatitude,
            ["pickupLongitude"] = dto.PickupLongitude,
            ["dropoffLatitude"] = dto.DropoffLatitude,
            ["dropoffLongitude"] = dto.DropoffLongitude
        };

        var state = await _dispatcher.DispatchAsync(command, context);
        return Ok(state);
    }

    [HttpPost("property/list")]
    public async Task<IActionResult> ListProperty([FromBody] PropertyListDto dto)
    {
        var command = new Domain.Application.Commands.ListPropertyCommand(
            Guid.NewGuid(), dto.OwnerId, dto.Title, dto.Description,
            new Shared.Location.GeoLocation(dto.Latitude, dto.Longitude), dto.MonthlyRent);

        var context = new Dictionary<string, object>
        {
            ["userId"] = dto.OwnerId.ToString(),
            ["title"] = dto.Title,
            ["description"] = dto.Description,
            ["monthlyRent"] = dto.MonthlyRent
        };

        var state = await _dispatcher.DispatchAsync(command, context);
        return Ok(state);
    }
}

public sealed record RideRequestDto(
    Guid UserId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude
);

public sealed record PropertyListDto(
    Guid OwnerId,
    string Title,
    string Description,
    double Latitude,
    double Longitude,
    decimal MonthlyRent
);
