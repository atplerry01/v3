namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyRolloutStore
{
    private readonly ConcurrentDictionary<(string PolicyId, string Version), PolicyRolloutConfig> _store = new();

    public void SetRolloutConfig(PolicyRolloutConfig config)
    {
        _store[(config.PolicyId, config.Version)] = config;
    }

    public PolicyRolloutConfig? GetRolloutConfig(string policyId, string version)
    {
        return _store.TryGetValue((policyId, version), out var config) ? config : null;
    }

    public IReadOnlyList<PolicyRolloutConfig> GetAllRollouts()
    {
        return _store.Values.ToList();
    }
}
