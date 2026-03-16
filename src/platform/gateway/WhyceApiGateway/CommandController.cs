namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.CommandSystem.Models;
using Whycespace.Contracts.Runtime;
using Whycespace.Systems.Midstream.WSS.Orchestration;
using CmdDispatcher = Whycespace.CommandSystem.Dispatcher.CommandDispatcher;

[ApiController]
[Route("api/commands")]
public sealed class CommandController : ControllerBase
{
    private readonly CmdDispatcher _commandDispatcher;
    private readonly WSSOrchestrator _orchestrator;

    public CommandController(CmdDispatcher commandDispatcher, WSSOrchestrator orchestrator)
    {
        _commandDispatcher = commandDispatcher;
        _orchestrator = orchestrator;
    }

    [HttpPost("ride/request")]
    public async Task<IActionResult> RequestRide([FromBody] RideRequestDto dto)
    {
        var context = new Dictionary<string, object>
        {
            ["userId"] = dto.UserId.ToString(),
            ["pickupLatitude"] = dto.PickupLatitude,
            ["pickupLongitude"] = dto.PickupLongitude,
            ["dropoffLatitude"] = dto.DropoffLatitude,
            ["dropoffLongitude"] = dto.DropoffLongitude
        };

        var envelope = new CommandEnvelope(
            CommandId: Guid.NewGuid(),
            CommandType: "RequestRideCommand",
            Payload: context,
            Timestamp: DateTimeOffset.UtcNow);

        var result = await _commandDispatcher.DispatchAsync(envelope);
        return Ok(result);
    }

    [HttpPost("property/list")]
    public async Task<IActionResult> ListProperty([FromBody] PropertyListDto dto)
    {
        var context = new Dictionary<string, object>
        {
            ["userId"] = dto.OwnerId.ToString(),
            ["title"] = dto.Title,
            ["description"] = dto.Description,
            ["monthlyRent"] = dto.MonthlyRent
        };

        var envelope = new CommandEnvelope(
            CommandId: Guid.NewGuid(),
            CommandType: "ListPropertyCommand",
            Payload: context,
            Timestamp: DateTimeOffset.UtcNow);

        var result = await _commandDispatcher.DispatchAsync(envelope);
        return Ok(result);
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
