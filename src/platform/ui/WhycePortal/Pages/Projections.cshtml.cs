namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class ProjectionsModel : PageModel
{
    private readonly GatewayClient _gateway;

    public ProjectionsModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement ProjectionNames { get; private set; }
    public JsonElement DriverLocations { get; private set; }
    public JsonElement PropertyListings { get; private set; }
    public JsonElement VaultBalances { get; private set; }
    public JsonElement Revenue { get; private set; }

    public async Task OnGetAsync()
    {
        var namesTask = _gateway.GetDevProjectionsAsync();
        var driversTask = _gateway.GetDriverLocationsAsync();
        var propertiesTask = _gateway.GetPropertyListingsAsync();
        var vaultsTask = _gateway.GetVaultBalancesAsync();
        var revenueTask = _gateway.GetRevenueAsync();

        await Task.WhenAll(namesTask, driversTask, propertiesTask, vaultsTask, revenueTask);

        ProjectionNames = namesTask.Result;
        DriverLocations = driversTask.Result;
        PropertyListings = propertiesTask.Result;
        VaultBalances = vaultsTask.Result;
        Revenue = revenueTask.Result;
    }
}
