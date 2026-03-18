using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Runtime.Persistence.Workflow;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;
using WfTemplate = Whycespace.Systems.Midstream.WSS.Definition.WorkflowTemplate;

namespace Whycespace.WSS.WorkflowValidation.Tests;

internal sealed class TemplateStoreAdapter : WorkflowTemplateEngine.ITemplateStore
{
    private readonly WorkflowTemplateStore _inner = new();
    public void Register(WfTemplate template) => _inner.Register(template);
    public WfTemplate Get(string templateId) => _inner.Get(templateId);
    public IReadOnlyCollection<WfTemplate> GetAll() => _inner.GetAll();
}

internal sealed class VersionStoreAdapter : WorkflowVersioningEngine.IVersionStore
{
    private readonly WorkflowVersionStore _inner = new();
    public void Store(WfDefinition workflow) => _inner.Store(workflow);
    public WfDefinition? Get(string workflowId, string version) => _inner.Get(workflowId, version);
    public WfDefinition? GetLatest(string workflowId) => _inner.GetLatest(workflowId);
    public IReadOnlyList<WfDefinition> GetVersions(string workflowId) => _inner.GetVersions(workflowId);
    public bool VersionExists(string workflowId, string version) => _inner.VersionExists(workflowId, version);
}
