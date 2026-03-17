namespace Whycespace.Engines.T0U.WhycePolicy.Monitoring;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyEvidenceRecorderEngine
{
    private readonly PolicyEvidenceStore _store;

    public PolicyEvidenceRecorderEngine(PolicyEvidenceStore store)
    {
        _store = store;
    }

    public PolicyEvidenceRecord RecordPolicyEvidence(
        string policyId,
        string actorId,
        string domain,
        string operation,
        bool allowed,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(actorId))
            throw new ArgumentException("Actor ID cannot be empty.");

        var now = DateTime.UtcNow;
        var evidenceId = GenerateEvidenceId(policyId, actorId, domain, operation, now);

        var record = new PolicyEvidenceRecord(
            evidenceId, policyId, actorId, domain, operation, allowed, reason, now);

        _store.RecordEvidence(record);
        return record;
    }

    public PolicyEvidenceRecord? GetEvidence(string evidenceId)
    {
        return _store.GetEvidence(evidenceId);
    }

    public IReadOnlyList<PolicyEvidenceRecord> GetAllEvidence()
    {
        return _store.GetAllEvidence();
    }

    private static string GenerateEvidenceId(string policyId, string actorId, string domain, string operation, DateTime timestamp)
    {
        var input = $"{policyId}:{actorId}:{domain}:{operation}:{timestamp:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"ev-{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }
}
