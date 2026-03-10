namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class SpvsModel : PageModel
{
    private readonly GatewayClient _gateway;

    public SpvsModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement VaultBalances { get; private set; }
    public JsonElement Revenue { get; private set; }

    public async Task OnGetAsync()
    {
        var vaultsTask = _gateway.GetVaultBalancesAsync();
        var revenueTask = _gateway.GetRevenueAsync();

        await Task.WhenAll(vaultsTask, revenueTask);

        VaultBalances = vaultsTask.Result;
        Revenue = revenueTask.Result;
    }
}
