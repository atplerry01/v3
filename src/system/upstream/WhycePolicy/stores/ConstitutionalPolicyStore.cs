namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class ConstitutionalPolicyStore
{
    private readonly ConcurrentDictionary<(string PolicyId, string Version), ConstitutionalPolicyRecord> _store = new();

    public void RegisterConstitutionalPolicy(ConstitutionalPolicyRecord record)
    {
        _store[(record.PolicyId, record.Version)] = record;
    }

    public bool IsProtectedPolicy(string policyId, string version)
    {
        return _store.ContainsKey((policyId, version));
    }

    public string? GetProtectionLevel(string policyId, string version)
    {
        return _store.TryGetValue((policyId, version), out var record) ? record.ProtectionLevel : null;
    }

    public ConstitutionalPolicyRecord? Get(string policyId, string version)
    {
        return _store.TryGetValue((policyId, version), out var record) ? record : null;
    }
}
