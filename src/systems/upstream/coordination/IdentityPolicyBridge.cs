namespace Whycespace.Systems.Upstream.Coordination;

using Whycespace.Contracts.Policy;

public sealed class IdentityPolicyBridge
{
    private readonly IPolicyEvaluator _policyEvaluator;

    private const string IdentityDomain = "whyceid";

    public IdentityPolicyBridge(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EnforceIdentityPolicy(
        string identityId,
        string operation,
        IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(identityId, IdentityDomain, operation, attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceIdentityCreation(
        string actorId, IReadOnlyDictionary<string, object> attributes)
    {
        return await EnforceIdentityPolicy(actorId, "identity.create", attributes);
    }

    public async Task<PolicyEvaluationResult> EnforceIdentityVerification(
        string identityId, IReadOnlyDictionary<string, object> attributes)
    {
        return await EnforceIdentityPolicy(identityId, "identity.verify", attributes);
    }

    public async Task<PolicyEvaluationResult> EnforceIdentityRevocation(
        string identityId, IReadOnlyDictionary<string, object> attributes)
    {
        return await EnforceIdentityPolicy(identityId, "identity.revoke", attributes);
    }
}
