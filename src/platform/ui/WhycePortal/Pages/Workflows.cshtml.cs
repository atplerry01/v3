namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class WorkflowsModel : PageModel
{
    private readonly GatewayClient _gateway;

    public WorkflowsModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement Workflows { get; private set; }

    public async Task OnGetAsync()
    {
        Workflows = await _gateway.GetWorkflowsAsync();
    }
}
