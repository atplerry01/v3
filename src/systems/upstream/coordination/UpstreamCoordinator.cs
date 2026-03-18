namespace Whycespace.Systems.Upstream.Coordination;

using Whycespace.Contracts.Policy;

public sealed class UpstreamCoordinator
{
    private readonly PolicyEnforcementBridge _policyBridge;
    private readonly IdentityPolicyBridge _identityBridge;
    private readonly GovernanceChainBridge _governanceChainBridge;

    public UpstreamCoordinator(
        PolicyEnforcementBridge policyBridge,
        IdentityPolicyBridge identityBridge,
        GovernanceChainBridge governanceChainBridge)
    {
        _policyBridge = policyBridge;
        _identityBridge = identityBridge;
        _governanceChainBridge = governanceChainBridge;
    }

    public async Task<PolicyEvaluationResult> EnforceUpstreamPolicy(
        string actorId, string domain, string operation, IReadOnlyDictionary<string, object> attributes)
    {
        return await _policyBridge.Enforce(actorId, domain, operation, attributes);
    }

    public async Task<PolicyEvaluationResult> EnforceIdentityOperation(
        string identityId, string operation, IReadOnlyDictionary<string, object> attributes)
    {
        return await _identityBridge.EnforceIdentityPolicy(identityId, operation, attributes);
    }

    public async Task AnchorGovernanceDecision(
        string proposalId, string outcome, IReadOnlyDictionary<string, object> evidence)
    {
        await _governanceChainBridge.AnchorDecision(proposalId, outcome, evidence);
    }
}
