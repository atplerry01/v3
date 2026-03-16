namespace Whycespace.Systems.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyMonitoringStore
{
    private readonly ConcurrentDictionary<string, PolicyMonitoringRecord> _records = new();

    public void RecordEvaluation(string policyId, string domain, bool allowed)
    {
        _records.AddOrUpdate(
            policyId,
            _ => new PolicyMonitoringRecord(
                policyId, domain,
                1,
                allowed ? 1 : 0,
                allowed ? 0 : 1,
                DateTime.UtcNow),
            (_, existing) => new PolicyMonitoringRecord(
                existing.PolicyId,
                domain,
                existing.Evaluations + 1,
                existing.AllowedCount + (allowed ? 1 : 0),
                existing.DeniedCount + (allowed ? 0 : 1),
                DateTime.UtcNow));
    }

    public PolicyMonitoringRecord? GetMonitoringData(string policyId)
    {
        _records.TryGetValue(policyId, out var record);
        return record;
    }

    public IReadOnlyList<PolicyMonitoringRecord> GetAllMonitoringData()
    {
        return _records.Values.ToList();
    }
}
