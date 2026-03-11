namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyVersionStore
{
    private readonly ConcurrentDictionary<string, List<PolicyVersion>> _store = new();

    public void Store(PolicyVersion version)
    {
        _store.AddOrUpdate(
            version.PolicyId,
            _ => new List<PolicyVersion> { version },
            (_, list) => { list.Add(version); return list; });
    }

    public IReadOnlyList<PolicyVersion> GetVersions(string policyId)
    {
        if (!_store.TryGetValue(policyId, out var versions))
            return Array.Empty<PolicyVersion>();

        return versions.OrderBy(v => v.Version).ToList();
    }

    public PolicyVersion? GetLatest(string policyId)
    {
        if (!_store.TryGetValue(policyId, out var versions) || versions.Count == 0)
            return null;

        return versions.MaxBy(v => v.Version);
    }

    public bool VersionExists(string policyId, int version)
    {
        if (!_store.TryGetValue(policyId, out var versions))
            return false;

        return versions.Any(v => v.Version == version);
    }
}
