namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceWorkflowEngine
{
    private readonly GovernanceWorkflowStore _workflowStore;
    private readonly GovernanceProposalStore _proposalStore;

    public GovernanceWorkflowEngine(
        GovernanceWorkflowStore workflowStore,
        GovernanceProposalStore proposalStore)
    {
        _workflowStore = workflowStore;
        _proposalStore = proposalStore;
    }

    public GovernanceWorkflow StartWorkflow(string workflowId, string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var workflow = new GovernanceWorkflow(
            workflowId,
            proposalId,
            WorkflowStage.Create,
            DateTime.UtcNow,
            CompletedAt: null);

        _workflowStore.Add(workflow);
        return workflow;
    }

    public GovernanceWorkflow AdvanceWorkflow(string workflowId)
    {
        var workflow = _workflowStore.Get(workflowId)
            ?? throw new KeyNotFoundException($"Workflow not found: {workflowId}");

        if (workflow.Stage == WorkflowStage.Completed)
            throw new InvalidOperationException("Workflow is already completed.");

        if (workflow.Stage == WorkflowStage.Execution)
            throw new InvalidOperationException("Workflow is in Execution stage. Use CompleteWorkflow to finalize.");

        var nextStage = (WorkflowStage)((int)workflow.Stage + 1);
        var updated = workflow with { Stage = nextStage };
        _workflowStore.Update(updated);
        return updated;
    }

    public GovernanceWorkflow CompleteWorkflow(string workflowId)
    {
        var workflow = _workflowStore.Get(workflowId)
            ?? throw new KeyNotFoundException($"Workflow not found: {workflowId}");

        if (workflow.Stage == WorkflowStage.Completed)
            throw new InvalidOperationException("Workflow is already completed.");

        if (workflow.Stage != WorkflowStage.Execution)
            throw new InvalidOperationException($"Workflow must be in Execution stage to complete. Current stage: {workflow.Stage}");

        var updated = workflow with
        {
            Stage = WorkflowStage.Completed,
            CompletedAt = DateTime.UtcNow
        };
        _workflowStore.Update(updated);
        return updated;
    }
}
