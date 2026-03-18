namespace Whycespace.Runtime.Versioning;

public sealed class RuntimeVersionManager
{
    private readonly Dictionary<string, string> _componentVersions = new();

    public void RegisterVersion(string componentId, string version)
    {
        _componentVersions[componentId] = version;
    }

    public string? GetVersion(string componentId)
    {
        _componentVersions.TryGetValue(componentId, out var version);
        return version;
    }

    public IReadOnlyDictionary<string, string> GetAllVersions()
    {
        return _componentVersions.AsReadOnly();
    }

    public bool IsCompatible(string componentId, string requiredVersion)
    {
        if (!_componentVersions.TryGetValue(componentId, out var currentVersion))
            return false;

        return CompatibilityMatrix.IsCompatible(currentVersion, requiredVersion);
    }
}
