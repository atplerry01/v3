namespace Whycespace.Runtime.EngineManifest.Validation;

using Whycespace.Runtime.EngineManifest.Models;

public sealed class RuntimeManifestValidator
{
    public void Validate(EngineRuntimeManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.EngineName))
            throw new InvalidOperationException("EngineName is required.");

        if (string.IsNullOrWhiteSpace(manifest.EngineType))
            throw new InvalidOperationException("EngineType is required.");

        if (string.IsNullOrWhiteSpace(manifest.InputContract))
            throw new InvalidOperationException("InputContract is required.");

        if (string.IsNullOrWhiteSpace(manifest.OutputContract))
            throw new InvalidOperationException("OutputContract is required.");

        if (string.IsNullOrWhiteSpace(manifest.Version))
            throw new InvalidOperationException("Version is required.");

        if (manifest.Capabilities is null || manifest.Capabilities.Count == 0)
            throw new InvalidOperationException("At least one capability is required.");
    }
}
