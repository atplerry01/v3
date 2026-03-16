namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyDependencyStore
{
    private readonly ConcurrentDictionary<string, List<PolicyDependency>> _store = new();

    public void Add(PolicyDependency dependency)
    {
        _store.AddOrUpdate(
            dependency.PolicyId,
            _ => new List<PolicyDependency> { dependency },
            (_, list) => { list.Add(dependency); return list; });
    }

    public IReadOnlyList<PolicyDependency> GetDependencies(string policyId)
    {
        if (!_store.TryGetValue(policyId, out var deps))
            return Array.Empty<PolicyDependency>();

        return deps.ToList();
    }

    public bool HasDependency(string policyId, string dependsOnPolicyId)
    {
        if (!_store.TryGetValue(policyId, out var deps))
            return false;

        return deps.Any(d => d.DependsOnPolicyId == dependsOnPolicyId);
    }

    public IReadOnlyList<string> GetAllPolicyIds()
    {
        return _store.Keys.ToList();
    }
}
