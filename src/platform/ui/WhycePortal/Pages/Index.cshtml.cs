namespace Whycespace.Platform.UI.WhycePortal.Pages;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Whycespace.Platform.UI.WhycePortal.Services;

public sealed class IndexModel : PageModel
{
    private readonly GatewayClient _gateway;

    public IndexModel(GatewayClient gateway) => _gateway = gateway;

    public JsonElement Engines { get; private set; }
    public JsonElement Clusters { get; private set; }
    public JsonElement Workflows { get; private set; }
    public JsonElement Events { get; private set; }

    public async Task OnGetAsync()
    {
        var enginesTask = _gateway.GetDevEnginesAsync();
        var clustersTask = _gateway.GetClustersAsync();
        var workflowsTask = _gateway.GetDevWorkflowsAsync();
        var eventsTask = _gateway.GetEventsAsync();

        await Task.WhenAll(enginesTask, clustersTask, workflowsTask, eventsTask);

        Engines = enginesTask.Result;
        Clusters = clustersTask.Result;
        Workflows = workflowsTask.Result;
        Events = eventsTask.Result;
    }
}
