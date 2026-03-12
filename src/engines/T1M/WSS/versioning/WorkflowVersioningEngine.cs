namespace Whycespace.Engines.T1M.WSS.Versioning;

using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;

public sealed class WorkflowVersioningEngine : IWorkflowVersioningEngine
{
    private readonly WorkflowVersionStore _store;

    public WorkflowVersioningEngine(WorkflowVersionStore store)
    {
        _store = store;
    }

    public WorkflowDefinition RegisterWorkflowVersion(WorkflowDefinition workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.WorkflowId))
            throw new ArgumentException("WorkflowId must not be empty.");

        if (string.IsNullOrWhiteSpace(workflow.Version))
            throw new ArgumentException("Version must not be empty.");

        if (!WorkflowVersionStore.IsValidSemanticVersion(workflow.Version))
            throw new ArgumentException($"Invalid semantic version format: '{workflow.Version}'. Expected Major.Minor.Patch (e.g. 1.0.0).");

        _store.Store(workflow);
        return workflow;
    }

    public WorkflowDefinition GetWorkflowVersion(string workflowId, string version)
    {
        return _store.Get(workflowId, version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for workflow: '{workflowId}'");
    }

    public WorkflowDefinition GetLatestWorkflow(string workflowId)
    {
        return _store.GetLatest(workflowId)
            ?? throw new KeyNotFoundException($"No versions found for workflow: '{workflowId}'");
    }

    public IReadOnlyList<WorkflowDefinition> ListWorkflowVersions(string workflowId)
    {
        return _store.GetVersions(workflowId);
    }

    public bool WorkflowVersionExists(string workflowId, string version)
    {
        return _store.VersionExists(workflowId, version);
    }
}
