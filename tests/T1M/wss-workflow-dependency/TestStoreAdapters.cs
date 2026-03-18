using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Runtime.Persistence.Workflow;
using WfDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;

namespace Whycespace.WSS.WorkflowDependency.Tests;

internal sealed class DefinitionStoreAdapter : WorkflowDependencyAnalyzer.IDefinitionStore
{
    private readonly WorkflowDefinitionStore _inner = new();
    public void Register(WfDefinition definition) => _inner.Register(definition);
    public IReadOnlyCollection<WfDefinition> GetAll() => _inner.GetAll();
}
