namespace Whycespace.Systems.Upstream.WhyceChain.Enforcement;

using Whycespace.Contracts.Policy;
using Whycespace.Systems.Upstream.WhyceChain.Policy;

public sealed class ChainEnforcementGuard
{
    private readonly ChainPolicyEnforcer _enforcer;

    public ChainEnforcementGuard(ChainPolicyEnforcer enforcer)
    {
        _enforcer = enforcer;
    }

    public async Task<PolicyEvaluationResult> GuardAppend(
        string actorId, string blockType, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceBlockAppend(actorId, blockType, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardEvidence(
        string actorId, string evidenceType, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceEvidenceAnchor(actorId, evidenceType, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardSnapshot(
        string actorId, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceSnapshotCreation(actorId, attributes);
    }
}
