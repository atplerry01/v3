namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class ClustersModel : PageModel
{
    private readonly GatewayClient _gateway;

    public ClustersModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement Clusters { get; private set; }

    public async Task OnGetAsync()
    {
        Clusters = await _gateway.GetClustersAsync();
    }
}
