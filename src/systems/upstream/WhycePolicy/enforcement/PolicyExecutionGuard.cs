namespace Whycespace.Systems.Upstream.WhycePolicy.Enforcement;

using Whycespace.Contracts.Policy;

public sealed class PolicyExecutionGuard
{
    private readonly PolicySelfEnforcer _enforcer;

    public PolicyExecutionGuard(PolicySelfEnforcer enforcer)
    {
        _enforcer = enforcer;
    }

    public async Task<PolicyEvaluationResult> GuardRegistration(
        string actorId, string policyDomain, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforcePolicyRegistration(actorId, policyDomain, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardLifecycleTransition(
        string actorId, string policyId, string targetState, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforcePolicyLifecycleChange(actorId, policyId, targetState, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardConstitutionalAmendment(
        string actorId, string policyId, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceConstitutionalAmendment(actorId, policyId, attributes);
    }
}
