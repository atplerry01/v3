namespace Whycespace.Systems.Upstream.WhycePolicy.Enforcement;

using Whycespace.Contracts.Policy;

public sealed class PolicySelfEnforcer
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public PolicySelfEnforcer(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EnforcePolicyRegistration(
        string actorId, string policyDomain, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"whycepolicy.registry.{policyDomain}", "register", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforcePolicyLifecycleChange(
        string actorId, string policyId, string targetState, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"whycepolicy.lifecycle.{policyId}", $"transition.{targetState}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceConstitutionalAmendment(
        string actorId, string policyId, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"whycepolicy.constitutional.{policyId}", "amend", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforcePolicySimulation(
        string actorId, string policyId, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"whycepolicy.simulation.{policyId}", "execute", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }
}
