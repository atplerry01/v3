namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkflowGraph", EngineTier.T1M, EngineKind.Decision, "WorkflowGraphRequest", typeof(EngineEvent))]
public sealed class WorkflowGraphRuntimeEngine : IEngine
{
    public string Name => "WorkflowGraph";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string;
        if (string.IsNullOrEmpty(workflowName))
            return Task.FromResult(EngineResult.Fail("Missing workflowName"));

        var stepCount = 0;
        if (context.Data.GetValueOrDefault("stepCount") is int sc)
            stepCount = sc;
        else if (context.Data.GetValueOrDefault("stepCount") is string scs && int.TryParse(scs, out var parsed))
            stepCount = parsed;

        var graphId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("WorkflowGraphBuilt", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["graphId"] = graphId.ToString(),
                    ["workflowName"] = workflowName,
                    ["stepCount"] = stepCount,
                    ["builtAt"] = DateTimeOffset.UtcNow.ToString("O")
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["graphId"] = graphId.ToString(),
                ["workflowName"] = workflowName,
                ["stepCount"] = stepCount
            }));
    }
}
