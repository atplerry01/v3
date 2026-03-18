namespace Whycespace.Systems.Upstream.WhyceChain.Policy;

using Whycespace.Contracts.Policy;

public sealed class ChainPolicyEnforcer
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public ChainPolicyEnforcer(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EnforceBlockAppend(
        string actorId, string blockType, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, "whycechain.block", $"append.{blockType}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceEvidenceAnchor(
        string actorId, string evidenceType, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, "whycechain.evidence", $"anchor.{evidenceType}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceSnapshotCreation(
        string actorId, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, "whycechain.snapshot", "create", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceReplication(
        string actorId, string targetNode, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"whycechain.replication.{targetNode}", "replicate", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }
}
