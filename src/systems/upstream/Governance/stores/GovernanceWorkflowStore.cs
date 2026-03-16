namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceWorkflowStore
{
    private readonly ConcurrentDictionary<string, GovernanceWorkflow> _workflows = new();

    public void Add(GovernanceWorkflow workflow)
    {
        if (!_workflows.TryAdd(workflow.WorkflowId, workflow))
            throw new InvalidOperationException($"Workflow already exists: {workflow.WorkflowId}");
    }

    public GovernanceWorkflow? Get(string workflowId)
    {
        _workflows.TryGetValue(workflowId, out var workflow);
        return workflow;
    }

    public void Update(GovernanceWorkflow workflow)
    {
        if (!_workflows.ContainsKey(workflow.WorkflowId))
            throw new KeyNotFoundException($"Workflow not found: {workflow.WorkflowId}");

        _workflows[workflow.WorkflowId] = workflow;
    }
}
