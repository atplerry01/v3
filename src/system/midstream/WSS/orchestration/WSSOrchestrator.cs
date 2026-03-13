namespace Whycespace.System.Midstream.WSS.Orchestration;

using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Mapping;
using Whycespace.WorkflowRuntime.Registry;
using WfRuntime = Whycespace.WorkflowRuntime.Runtime.WorkflowRuntime;

public sealed class WSSOrchestrator
{
    private readonly WorkflowMapper _mapper;
    private readonly WorkflowOrchestrator _orchestrator;
    private readonly WfRuntime? _workflowRuntime;

    public WSSOrchestrator(WorkflowMapper mapper, WorkflowOrchestrator orchestrator)
    {
        _mapper = mapper;
        _orchestrator = orchestrator;
    }

    public WSSOrchestrator(WorkflowMapper mapper, WorkflowOrchestrator orchestrator, WfRuntime workflowRuntime)
        : this(mapper, orchestrator)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task<WorkflowState> StartWorkflowAsync(
        string workflowName,
        IReadOnlyDictionary<string, object> context)
    {
        var definition = _mapper.Resolve(workflowName)
            ?? throw new InvalidOperationException($"Unknown workflow: {workflowName}");

        // Use WorkflowRuntime if available, falling back to direct orchestration
        if (_workflowRuntime is not null)
        {
            var request = new WorkflowExecutionRequest(workflowName, context);
            var result = await _workflowRuntime.ExecuteAsync(request);

            var graph = definition.BuildGraph();
            var status = result.Success ? WorkflowStatus.Completed : WorkflowStatus.Failed;
            return new WorkflowState(
                graph.WorkflowId, graph.Steps[^1].StepId, status,
                result.Output, DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }

        var wfGraph = definition.BuildGraph();
        return await _orchestrator.ExecuteWorkflowAsync(wfGraph, context);
    }
}
