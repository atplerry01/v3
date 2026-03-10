namespace Whycespace.System.Midstream.WSS.Orchestration;

using Whycespace.Runtime.Workflow;
using Whycespace.Shared.Workflow;
using Whycespace.System.Midstream.WSS.Mapping;

public sealed class WSSOrchestrator
{
    private readonly WorkflowMapper _mapper;
    private readonly WorkflowOrchestrator _orchestrator;

    public WSSOrchestrator(WorkflowMapper mapper, WorkflowOrchestrator orchestrator)
    {
        _mapper = mapper;
        _orchestrator = orchestrator;
    }

    public async Task<WorkflowState> StartWorkflowAsync(
        string workflowName,
        IReadOnlyDictionary<string, object> context)
    {
        var definition = _mapper.Resolve(workflowName)
            ?? throw new InvalidOperationException($"Unknown workflow: {workflowName}");

        var graph = definition.BuildGraph();
        return await _orchestrator.ExecuteWorkflowAsync(graph, context);
    }
}
