namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Stores;

public sealed class WorkflowVersioningEngine
{
    private readonly WorkflowVersionStore _versionStore;
    private readonly WorkflowDefinitionStore _definitionStore;

    public WorkflowVersioningEngine(WorkflowVersionStore versionStore, WorkflowDefinitionStore definitionStore)
    {
        _versionStore = versionStore;
        _definitionStore = definitionStore;
    }

    public WorkflowVersion CreateVersion(string workflowId, int version)
    {
        _definitionStore.Get(workflowId);

        if (_versionStore.VersionExists(workflowId, version))
            throw new InvalidOperationException($"Version {version} already exists for workflow: {workflowId}");

        var workflowVersion = new WorkflowVersion(
            workflowId,
            version,
            WorkflowVersionStatus.Draft,
            DateTimeOffset.UtcNow);

        _versionStore.Store(workflowVersion);
        return workflowVersion;
    }

    public WorkflowVersion ActivateVersion(string workflowId, int version)
    {
        var versions = _versionStore.GetVersions(workflowId);
        var target = versions.FirstOrDefault(v => v.Version == version)
            ?? throw new KeyNotFoundException($"Version {version} not found for workflow: {workflowId}");

        if (target.Status == WorkflowVersionStatus.Active)
            return target;

        var currentActive = _versionStore.GetActive(workflowId);
        if (currentActive is not null)
        {
            _versionStore.Update(currentActive with { Status = WorkflowVersionStatus.Superseded });
        }

        var activated = target with { Status = WorkflowVersionStatus.Active };
        _versionStore.Update(activated);
        return activated;
    }

    public WorkflowVersion GetActiveVersion(string workflowId)
    {
        return _versionStore.GetActive(workflowId)
            ?? throw new KeyNotFoundException($"No active version for workflow: {workflowId}");
    }

    public IReadOnlyList<WorkflowVersion> GetVersions(string workflowId)
    {
        return _versionStore.GetVersions(workflowId);
    }
}
