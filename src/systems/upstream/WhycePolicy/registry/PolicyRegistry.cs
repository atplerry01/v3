namespace Whycespace.Systems.Upstream.WhycePolicy.Registry;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyRegistry
{
    private readonly ConcurrentDictionary<string, PolicyRegistryEntry> _entries = new();
    private readonly ConcurrentDictionary<string, List<PolicyVersionRecord>> _versions = new();
    private readonly object _lock = new();

    public void RegisterPolicy(PolicyDefinition policy)
    {
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (_entries.TryGetValue(policy.PolicyId, out var existing))
            {
                var nextVersion = existing.CurrentVersion + 1;

                var updated = existing with
                {
                    CurrentVersion = nextVersion,
                    UpdatedAt = now
                };

                _entries[policy.PolicyId] = updated;

                var versionRecord = new PolicyVersionRecord(
                    policy.PolicyId,
                    nextVersion,
                    policy,
                    now,
                    CreatedBy: string.Empty
                );

                _versions.AddOrUpdate(
                    policy.PolicyId,
                    _ => new List<PolicyVersionRecord> { versionRecord },
                    (_, list) => { list.Add(versionRecord); return list; });
            }
            else
            {
                var entry = new PolicyRegistryEntry(
                    PolicyId: policy.PolicyId,
                    PolicyName: policy.Name,
                    Domain: policy.TargetDomain,
                    Priority: 0,
                    LifecycleState: PolicyLifecycleState.Active,
                    CurrentVersion: 1,
                    CreatedAt: now,
                    UpdatedAt: now
                );

                if (!_entries.TryAdd(policy.PolicyId, entry))
                    throw new InvalidOperationException($"Policy '{policy.PolicyId}' already exists.");

                var versionRecord = new PolicyVersionRecord(
                    policy.PolicyId,
                    1,
                    policy,
                    now,
                    CreatedBy: string.Empty
                );

                _versions[policy.PolicyId] = new List<PolicyVersionRecord> { versionRecord };
            }
        }
    }

    public PolicyRegistryEntry? GetPolicyById(string policyId)
    {
        _entries.TryGetValue(policyId, out var entry);
        return entry;
    }

    public IReadOnlyList<PolicyRegistryEntry> GetPoliciesByDomain(string domain)
    {
        return _entries.Values
            .Where(e => string.Equals(e.Domain, domain, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<PolicyRegistryEntry> GetActivePolicies()
    {
        return _entries.Values
            .Where(e => e.LifecycleState == PolicyLifecycleState.Active)
            .ToList();
    }

    public IReadOnlyList<PolicyVersionRecord> GetPolicyVersions(string policyId)
    {
        if (!_versions.TryGetValue(policyId, out var versions))
            return Array.Empty<PolicyVersionRecord>();

        lock (_lock)
        {
            return versions.OrderBy(v => v.Version).ToList();
        }
    }
}
