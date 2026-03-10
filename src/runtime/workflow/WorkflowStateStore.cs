namespace Whycespace.Runtime.Workflow;

using Whycespace.Contracts.Workflows;

public sealed class WorkflowStateStore
{
    private readonly Dictionary<string, WorkflowState> _states = new();

    public void Save(WorkflowState state)
    {
        _states[state.WorkflowId] = state;
    }

    public WorkflowState? Get(string workflowId)
    {
        _states.TryGetValue(workflowId, out var state);
        return state;
    }

    public IReadOnlyList<WorkflowState> GetAll() => _states.Values.ToList();
}
