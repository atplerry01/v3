namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicyAuditEngine
{
    private readonly PolicyEvidenceStore _evidenceStore;

    public PolicyAuditEngine(PolicyEvidenceStore evidenceStore)
    {
        _evidenceStore = evidenceStore;
    }

    public PolicyAuditReport AuditPolicy(PolicyAuditQuery query)
    {
        var records = _evidenceStore.GetAllEvidence().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.PolicyId))
            records = records.Where(r => r.PolicyId == query.PolicyId);

        if (!string.IsNullOrWhiteSpace(query.ActorId))
            records = records.Where(r => r.ActorId == query.ActorId);

        if (!string.IsNullOrWhiteSpace(query.Domain))
            records = records.Where(r => r.Domain == query.Domain);

        if (query.From.HasValue)
            records = records.Where(r => r.RecordedAt >= query.From.Value);

        if (query.To.HasValue)
            records = records.Where(r => r.RecordedAt <= query.To.Value);

        var filtered = records.ToList();

        return new PolicyAuditReport(filtered, filtered.Count, DateTime.UtcNow);
    }
}
