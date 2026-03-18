using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Runtime.Persistence.Workflow;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;
using WfTemplate = Whycespace.Systems.Midstream.WSS.Models.WorkflowTemplate;
using WfRegistryEntry = Whycespace.Systems.Midstream.WSS.Models.WorkflowRegistryEntry;

namespace Whycespace.WSS.WorkflowDefinition.Tests;

internal sealed class TemplateStoreAdapter : WorkflowTemplateEngine.ITemplateStore
{
    private readonly WorkflowTemplateStore _inner = new();
    public void Register(WfTemplate template) => _inner.Register(template);
    public WfTemplate Get(string templateId) => _inner.Get(templateId);
    public IReadOnlyCollection<WfTemplate> GetAll() => _inner.GetAll();
}

internal sealed class RegistryStoreAdapter : WorkflowRegistryEngine.IRegistryStore
{
    private readonly WorkflowRegistryStore _inner = new();
    public void Register(WfRegistryEntry entry) => _inner.Register(entry);
    public WfRegistryEntry Get(string workflowId) => _inner.Get(workflowId);
    public IReadOnlyCollection<WfRegistryEntry> GetAll() => _inner.GetAll();
}

internal sealed class DefinitionLookupAdapter : WorkflowRegistryEngine.IDefinitionLookup
{
    private readonly WorkflowDefinitionStore _inner;
    public DefinitionLookupAdapter(WorkflowDefinitionStore inner) { _inner = inner; }
    public WfDefinition Get(string workflowId) => _inner.Get(workflowId);
}

internal sealed class DefinitionStoreAdapter : WorkflowDependencyAnalyzer.IDefinitionStore
{
    private readonly WorkflowDefinitionStore _inner;
    public DefinitionStoreAdapter(WorkflowDefinitionStore inner) { _inner = inner; }
    public void Register(WfDefinition definition) => _inner.Register(definition);
    public IReadOnlyCollection<WfDefinition> GetAll() => _inner.GetAll();
}
