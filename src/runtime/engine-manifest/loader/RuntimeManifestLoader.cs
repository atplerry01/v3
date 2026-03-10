namespace Whycespace.EngineManifest.Loader;

using Whycespace.EngineManifest.Models;
using Whycespace.EngineManifest.Registry;

public sealed class RuntimeManifestLoader
{
    private readonly EngineManifestRegistry _registry;

    public RuntimeManifestLoader(EngineManifestRegistry registry)
    {
        _registry = registry;
    }

    public void Load(IEnumerable<EngineRuntimeManifest> manifests)
    {
        foreach (var manifest in manifests)
        {
            _registry.Register(manifest);
        }
    }
}
