namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Stores;

public sealed class WorkflowDefinitionEngine
{
    private readonly WorkflowDefinitionStore _store;

    public WorkflowDefinitionEngine(WorkflowDefinitionStore store)
    {
        _store = store;
    }

    public WorkflowDefinition RegisterWorkflow(string workflowId, string name, string description, int version, IReadOnlyList<WorkflowStep> steps)
    {
        var definition = new WorkflowDefinition(
            workflowId,
            name,
            description,
            version,
            steps,
            DateTimeOffset.UtcNow);

        _store.Register(definition);
        return definition;
    }

    public WorkflowDefinition GetWorkflow(string workflowId)
    {
        return _store.Get(workflowId);
    }

    public IReadOnlyCollection<WorkflowDefinition> ListWorkflows()
    {
        return _store.GetAll();
    }
}
