namespace Whycespace.Runtime.Dispatcher.WSS;

using Whycespace.Contracts.Commands;
using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Orchestration;
using Whycespace.Runtime.EventFabricRuntime.WSS;

public sealed class CommandDispatcher
{
    private readonly WorkflowRouter _router;
    private readonly WSSOrchestrator _orchestrator;

    public CommandDispatcher(WorkflowRouter router, WSSOrchestrator orchestrator)
    {
        _router = router;
        _orchestrator = orchestrator;
    }

    public async Task<WorkflowState> DispatchAsync(ICommand command, IReadOnlyDictionary<string, object> context)
    {
        var workflowName = _router.ResolveWorkflow(command)
            ?? throw new InvalidOperationException($"No workflow mapped for command: {command.GetType().Name}");

        return await _orchestrator.StartWorkflowAsync(workflowName, context);
    }
}
