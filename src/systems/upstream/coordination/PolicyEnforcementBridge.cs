namespace Whycespace.Systems.Upstream.Coordination;

using Whycespace.Contracts.Policy;

public sealed class PolicyEnforcementBridge
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public PolicyEnforcementBridge(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> Enforce(
        string actorId,
        string domain,
        string operation,
        IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, domain, operation, attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<bool> IsPermitted(
        string actorId,
        string domain,
        string operation,
        IReadOnlyDictionary<string, object>? attributes = null)
    {
        var result = await Enforce(actorId, domain, operation, attributes ?? new Dictionary<string, object>());
        return result.IsPermitted;
    }
}
