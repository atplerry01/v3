namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class EventsModel : PageModel
{
    private readonly GatewayClient _gateway;

    public EventsModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement Events { get; private set; }

    public async Task OnGetAsync()
    {
        Events = await _gateway.GetEventsAsync();
    }
}
