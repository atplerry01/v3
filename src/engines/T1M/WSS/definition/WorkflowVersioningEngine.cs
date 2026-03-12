namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Engines.T1M.WSS.Versioning;
using Whycespace.System.Midstream.WSS.Models;
using Whycespace.Engines.T1M.WSS.Stores;

public sealed class WorkflowVersioningEngine
{
    private readonly WorkflowVersionStore _versionStore;
    private readonly WorkflowDefinitionStore _definitionStore;
    private readonly IWorkflowVersioningEngine _versioningEngine;

    public WorkflowVersioningEngine(WorkflowVersionStore versionStore, WorkflowDefinitionStore definitionStore)
    {
        _versionStore = versionStore;
        _definitionStore = definitionStore;
        _versioningEngine = new Whycespace.Engines.T1M.WSS.Versioning.WorkflowVersioningEngine(versionStore);
    }

    public WorkflowDefinition RegisterWorkflowVersion(WorkflowDefinition workflow)
    {
        return _versioningEngine.RegisterWorkflowVersion(workflow);
    }

    public WorkflowDefinition GetWorkflowVersion(string workflowId, string version)
    {
        return _versioningEngine.GetWorkflowVersion(workflowId, version);
    }

    public WorkflowDefinition GetLatestWorkflow(string workflowId)
    {
        return _versioningEngine.GetLatestWorkflow(workflowId);
    }

    public IReadOnlyList<WorkflowDefinition> ListWorkflowVersions(string workflowId)
    {
        return _versioningEngine.ListWorkflowVersions(workflowId);
    }

    public bool WorkflowVersionExists(string workflowId, string version)
    {
        return _versioningEngine.WorkflowVersionExists(workflowId, version);
    }
}
