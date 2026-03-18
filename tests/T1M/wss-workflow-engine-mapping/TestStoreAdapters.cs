using Whycespace.Engines.T1M.WSS.Step;
using Whycespace.Runtime.Persistence.Workflow;

namespace Whycespace.WSS.WorkflowEngineMapping.Tests;

internal sealed class MappingStoreAdapter : WorkflowStepEngineMapper.IMappingStore
{
    private readonly WorkflowEngineMappingStore _inner = new();
    public void Register(string engineName, string runtimeIdentifier) => _inner.Register(engineName, runtimeIdentifier);
    public bool TryGet(string engineName, out string runtimeIdentifier) => _inner.TryGet(engineName, out runtimeIdentifier);
    public bool Exists(string engineName) => _inner.Exists(engineName);
    public IReadOnlyDictionary<string, string> GetAll() => _inner.GetAll();
}
