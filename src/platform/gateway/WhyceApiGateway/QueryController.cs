namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Projections.Queries;
using Whycespace.Runtime.Workflow;

[ApiController]
[Route("api/queries")]
public sealed class QueryController : ControllerBase
{
    private readonly ProjectionQueryService _queryService;
    private readonly WorkflowStateStore _workflowStateStore;

    public QueryController(
        ProjectionQueryService queryService,
        WorkflowStateStore workflowStateStore)
    {
        _queryService = queryService;
        _workflowStateStore = workflowStateStore;
    }

    [HttpGet("drivers/locations")]
    public async Task<IActionResult> GetDriverLocations([FromQuery] string? driverId)
    {
        if (driverId is null)
            return BadRequest(new { error = "driverId required" });

        var result = await _queryService.GetAsync($"driver:{driverId}");
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("properties/listings")]
    public async Task<IActionResult> GetPropertyListings([FromQuery] string? propertyId)
    {
        if (propertyId is null)
            return BadRequest(new { error = "propertyId required" });

        var result = await _queryService.GetAsync($"property:{propertyId}");
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("vaults/balances")]
    public async Task<IActionResult> GetVaultBalances([FromQuery] string? vaultId)
    {
        if (vaultId is null)
            return BadRequest(new { error = "vaultId required" });

        var result = await _queryService.GetAsync($"vault:{vaultId}");
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] string? aggregateId)
    {
        if (aggregateId is null)
            return BadRequest(new { error = "aggregateId required" });

        var result = await _queryService.GetAsync($"revenue:{aggregateId}");
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("workflows")]
    public IActionResult GetWorkflows() => Ok(_workflowStateStore.GetAll());
}
