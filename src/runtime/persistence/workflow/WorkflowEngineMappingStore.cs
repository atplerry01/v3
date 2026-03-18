namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;

public sealed class WorkflowEngineMappingStore
{
    private readonly ConcurrentDictionary<string, string> _mappings = new();

    public void Register(string engineName, string runtimeIdentifier)
    {
        if (!_mappings.TryAdd(engineName, runtimeIdentifier))
            throw new InvalidOperationException($"Engine mapping already exists: '{engineName}'");
    }

    public bool TryGet(string engineName, out string runtimeIdentifier)
    {
        return _mappings.TryGetValue(engineName, out runtimeIdentifier!);
    }

    public bool Exists(string engineName)
    {
        return _mappings.ContainsKey(engineName);
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>(_mappings);
    }
}
