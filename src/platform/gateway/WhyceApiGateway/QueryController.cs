namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Runtime.Projections;
using Whycespace.Runtime.Workflow;

[ApiController]
[Route("api/queries")]
public sealed class QueryController : ControllerBase
{
    private readonly DriverLocationProjection _driverLocationProjection;
    private readonly PropertyListingProjection _propertyListingProjection;
    private readonly VaultBalanceProjection _vaultBalanceProjection;
    private readonly RevenueProjection _revenueProjection;
    private readonly WorkflowStateStore _workflowStateStore;

    public QueryController(
        DriverLocationProjection driverLocationProjection,
        PropertyListingProjection propertyListingProjection,
        VaultBalanceProjection vaultBalanceProjection,
        RevenueProjection revenueProjection,
        WorkflowStateStore workflowStateStore)
    {
        _driverLocationProjection = driverLocationProjection;
        _propertyListingProjection = propertyListingProjection;
        _vaultBalanceProjection = vaultBalanceProjection;
        _revenueProjection = revenueProjection;
        _workflowStateStore = workflowStateStore;
    }

    [HttpGet("drivers/locations")]
    public IActionResult GetDriverLocations() => Ok(_driverLocationProjection.GetLocations());

    [HttpGet("properties/listings")]
    public IActionResult GetPropertyListings() => Ok(_propertyListingProjection.GetListings());

    [HttpGet("vaults/balances")]
    public IActionResult GetVaultBalances() => Ok(_vaultBalanceProjection.GetBalances());

    [HttpGet("revenue")]
    public IActionResult GetRevenue() => Ok(_revenueProjection.GetRevenues());

    [HttpGet("workflows")]
    public IActionResult GetWorkflows() => Ok(_workflowStateStore.GetAll());
}
