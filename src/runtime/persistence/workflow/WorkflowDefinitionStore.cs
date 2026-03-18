namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WorkflowDefinitionStore
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();

    public void Register(WorkflowDefinition definition)
    {
        if (!_definitions.TryAdd(definition.WorkflowId, definition))
            throw new InvalidOperationException($"Duplicate workflow: {definition.WorkflowId}");
    }

    public WorkflowDefinition Get(string workflowId)
    {
        if (!_definitions.TryGetValue(workflowId, out var definition))
            throw new KeyNotFoundException($"Workflow definition not found: {workflowId}");
        return definition;
    }

    public IReadOnlyCollection<WorkflowDefinition> GetAll()
    {
        return _definitions.Values.ToList();
    }
}
