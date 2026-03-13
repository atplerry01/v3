namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicyMonitoringEngine
{
    private readonly PolicyMonitoringStore _store;

    public PolicyMonitoringEngine(PolicyMonitoringStore store)
    {
        _store = store;
    }

    public void RecordPolicyEvaluation(string policyId, string domain, bool allowed)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain cannot be empty.");

        _store.RecordEvaluation(policyId, domain, allowed);
    }

    public PolicyMonitoringRecord? GetPolicyMetrics(string policyId)
    {
        return _store.GetMonitoringData(policyId);
    }

    public IReadOnlyList<PolicyMonitoringRecord> GetAllMetrics()
    {
        return _store.GetAllMonitoringData();
    }
}
