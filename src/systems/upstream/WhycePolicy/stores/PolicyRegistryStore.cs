namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyRegistryStore
{
    private readonly ConcurrentDictionary<string, List<PolicyRecord>> _store = new();

    public void Register(PolicyRecord record)
    {
        _store.AddOrUpdate(
            record.PolicyId,
            _ => new List<PolicyRecord> { record },
            (_, list) => { list.Add(record); return list; });
    }

    public PolicyRecord? Get(string policyId)
    {
        if (!_store.TryGetValue(policyId, out var records) || records.Count == 0)
            return null;

        return records
            .Where(r => r.Status == PolicyStatus.Active)
            .MaxBy(r => r.Version);
    }

    public PolicyRecord? GetVersion(string policyId, int version)
    {
        if (!_store.TryGetValue(policyId, out var records))
            return null;

        return records.Find(r => r.Version == version);
    }

    public IReadOnlyList<PolicyRecord> GetAll()
    {
        return _store.Values.SelectMany(r => r).ToList();
    }

    public bool Exists(string policyId)
    {
        return _store.ContainsKey(policyId);
    }

    public int GetLatestVersion(string policyId)
    {
        if (!_store.TryGetValue(policyId, out var records) || records.Count == 0)
            return 0;

        return records.Max(r => r.Version);
    }
}
