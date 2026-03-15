namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkflowGraph", EngineTier.T1M, EngineKind.Decision, "WorkflowGraphCommand", typeof(EngineEvent))]
public sealed class WorkflowGraphRuntimeEngine : IEngine
{
    private readonly WorkflowGraphEngine _graphEngine = new();

    public string Name => "WorkflowGraph";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string;
        var workflowVersion = context.Data.GetValueOrDefault("workflowVersion") as string ?? "1.0.0";

        if (string.IsNullOrEmpty(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        if (string.IsNullOrEmpty(workflowName))
            return Task.FromResult(EngineResult.Fail("Missing workflowName"));

        var steps = ParseStepsFromContext(context.Data);
        if (steps.Count == 0)
            return Task.FromResult(EngineResult.Fail("Missing or empty workflowSteps"));

        var command = new WorkflowGraphCommand(workflowId, workflowName, workflowVersion, steps);
        var result = _graphEngine.BuildExecutionGraph(command);

        if (!result.Success)
            return Task.FromResult(EngineResult.Fail(result.ErrorMessage!));

        var graph = result.Graph!;

        var events = new[]
        {
            EngineEvent.Create("WorkflowGraphBuilt", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["workflowId"] = workflowId,
                    ["workflowName"] = workflowName,
                    ["nodeCount"] = graph.Nodes.Count,
                    ["edgeCount"] = graph.Edges.Count,
                    ["parallelGroupCount"] = graph.ParallelGroups.Count,
                    ["topic"] = "whyce.wss.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["workflowId"] = workflowId,
                ["workflowName"] = workflowName,
                ["nodeCount"] = graph.Nodes.Count,
                ["edgeCount"] = graph.Edges.Count,
                ["executionOrder"] = string.Join(",", graph.ExecutionOrder),
                ["parallelGroupCount"] = graph.ParallelGroups.Count
            }));
    }

    private static List<WorkflowGraphStepInput> ParseStepsFromContext(IReadOnlyDictionary<string, object> data)
    {
        if (data.GetValueOrDefault("workflowSteps") is IReadOnlyList<WorkflowGraphStepInput> typedSteps)
            return typedSteps.ToList();

        return new List<WorkflowGraphStepInput>();
    }
}
