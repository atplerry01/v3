namespace Whycespace.Runtime.EngineManifest.Loader;

using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Runtime.EngineManifest.Registry;

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
