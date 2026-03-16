namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyLifecycleStore
{
    private readonly ConcurrentDictionary<(string PolicyId, string Version), List<PolicyLifecycleRecord>> _store = new();

    public void SetLifecycleState(PolicyLifecycleRecord record)
    {
        _store.AddOrUpdate(
            (record.PolicyId, record.Version),
            _ => new List<PolicyLifecycleRecord> { record },
            (_, list) => { list.Add(record); return list; });
    }

    public PolicyLifecycleRecord? GetLifecycleState(string policyId, string version)
    {
        if (!_store.TryGetValue((policyId, version), out var records) || records.Count == 0)
            return null;

        return records[^1];
    }

    public IReadOnlyList<PolicyLifecycleRecord> GetLifecycleHistory(string policyId, string version)
    {
        if (!_store.TryGetValue((policyId, version), out var records))
            return Array.Empty<PolicyLifecycleRecord>();

        return records.ToList();
    }
}
