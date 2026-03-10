namespace Whycespace.System.Midstream.WSS.Mapping;

using Whycespace.System.Midstream.WSS.Contracts;

public sealed class WorkflowMapper
{
    private readonly Dictionary<string, IWorkflowDefinition> _definitions = new();

    public void Register(IWorkflowDefinition definition)
    {
        _definitions[definition.WorkflowName] = definition;
    }

    public IWorkflowDefinition? Resolve(string workflowName)
    {
        _definitions.TryGetValue(workflowName, out var definition);
        return definition;
    }

    public IReadOnlyList<string> GetRegisteredWorkflows() => _definitions.Keys.ToList();
}
