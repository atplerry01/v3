namespace Whycespace.RuntimeGovernance;

using Whycespace.Runtime.EngineManifest.Models;

public sealed record EngineRegistryEntry(
    string EngineName,
    string EngineVersion,
    IReadOnlyCollection<string> SupportedCommandTypes,
    string ClusterDomain,
    EngineTier ExecutionTier);

public sealed class EngineCapabilityRegistry
{
    private readonly Dictionary<string, EngineRegistryEntry> _engines = new(StringComparer.OrdinalIgnoreCase);

    public void Register(EngineRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var key = BuildKey(entry.EngineName, entry.EngineVersion);
        _engines[key] = entry;
    }

    public EngineRegistryEntry? GetEngine(string engineName, string engineVersion)
    {
        var key = BuildKey(engineName, engineVersion);
        return _engines.GetValueOrDefault(key);
    }

    public bool EngineExists(string engineName, string engineVersion)
    {
        return _engines.ContainsKey(BuildKey(engineName, engineVersion));
    }

    public IReadOnlyCollection<EngineRegistryEntry> GetAllEngines() =>
        _engines.Values.ToList().AsReadOnly();

    private static string BuildKey(string engineName, string engineVersion) =>
        $"{engineName}:{engineVersion}";
}
