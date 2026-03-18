namespace Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Versioning;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyVersionEngine
{
    private readonly PolicyVersionStore _store;

    public PolicyVersionEngine(PolicyVersionStore store)
    {
        _store = store;
    }

    public PolicyVersion CreateVersion(string policyId, int version)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID cannot be empty.");

        if (version < 1)
            throw new ArgumentException("Version must be a positive integer.");

        if (_store.VersionExists(policyId, version))
            throw new InvalidOperationException(
                $"Version {version} already exists for policy '{policyId}'.");

        var policyVersion = new PolicyVersion(
            policyId,
            version,
            DateTime.UtcNow,
            PolicyStatus.Active
        );

        _store.Store(policyVersion);
        return policyVersion;
    }

    public PolicyVersion GetLatestVersion(string policyId)
    {
        var latest = _store.GetLatest(policyId);
        if (latest is null)
            throw new KeyNotFoundException($"No versions found for policy '{policyId}'.");
        return latest;
    }

    public IReadOnlyList<PolicyVersion> GetVersions(string policyId)
    {
        return _store.GetVersions(policyId);
    }

    public int CompareVersions(string policyId, int versionA, int versionB)
    {
        var versions = _store.GetVersions(policyId);

        var a = versions.FirstOrDefault(v => v.Version == versionA)
            ?? throw new KeyNotFoundException($"Version {versionA} not found for policy '{policyId}'.");

        var b = versions.FirstOrDefault(v => v.Version == versionB)
            ?? throw new KeyNotFoundException($"Version {versionB} not found for policy '{policyId}'.");

        return a.Version.CompareTo(b.Version);
    }
}
