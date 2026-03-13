namespace Whycespace.EngineManifest.Registry;

using Whycespace.EngineManifest.Models;

public sealed class EngineManifestRegistry
{
    private readonly Dictionary<string, EngineRuntimeManifest> _manifests = new();

    public void Register(EngineRuntimeManifest manifest)
    {
        if (_manifests.ContainsKey(manifest.EngineName))
            throw new InvalidOperationException($"Engine '{manifest.EngineName}' already registered.");

        _manifests[manifest.EngineName] = manifest;
    }

    public EngineRuntimeManifest Get(string engineName)
    {
        if (!_manifests.TryGetValue(engineName, out var manifest))
            throw new InvalidOperationException($"Engine '{engineName}' not found.");

        return manifest;
    }

    public IReadOnlyCollection<EngineRuntimeManifest> GetAll()
    {
        return _manifests.Values.ToList().AsReadOnly();
    }

    public bool Contains(string engineName) => _manifests.ContainsKey(engineName);

    public int Count => _manifests.Count;
}
