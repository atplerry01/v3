namespace Whycespace.Systems.Upstream.WhycePolicy.Enforcement;

using Whycespace.Systems.Upstream.WhycePolicy.Registry;
using ContractPolicy = Whycespace.Contracts.Policy;

public sealed class PolicyEnforcementEngine : ContractPolicy.IPolicyEvaluator
{
    private readonly PolicyRegistry _registry;

    public PolicyEnforcementEngine(PolicyRegistry registry)
    {
        _registry = registry;
    }

    private const int CriticalPriority = 100;

    public Task<ContractPolicy.PolicyEvaluationResult> EvaluateAsync(ContractPolicy.PolicyContext context)
    {
        var matchingPolicies = _registry.GetPoliciesByDomain(context.ResourceId);
        if (matchingPolicies.Count == 0)
        {
            return Task.FromResult(ContractPolicy.PolicyEvaluationResult.Permit(
                new ContractPolicy.PolicyDecision("default.allow", true, "No policies matched — default allow", DateTimeOffset.UtcNow)));
        }

        var decisions = new List<ContractPolicy.PolicyDecision>();
        var violations = new List<string>();

        foreach (var policy in matchingPolicies)
        {
            var decision = EvaluatePolicy(policy, context);
            decisions.Add(decision);

            if (!decision.IsAllowed)
                violations.Add($"Policy '{policy.PolicyId}' denied: {decision.Reason}");
        }

        var isPermitted = violations.Count == 0;
        return Task.FromResult(isPermitted
            ? ContractPolicy.PolicyEvaluationResult.Permit(decisions.ToArray())
            : ContractPolicy.PolicyEvaluationResult.Deny(violations, decisions.ToArray()));
    }

    private static ContractPolicy.PolicyDecision EvaluatePolicy(
        PolicyRegistryEntry policy, ContractPolicy.PolicyContext context)
    {
        var isAllowed = policy.Priority < CriticalPriority || context.Attributes.ContainsKey("override");
        var reason = isAllowed
            ? $"Policy '{policy.PolicyId}' permits action '{context.Action}'"
            : $"Policy '{policy.PolicyId}' denies action '{context.Action}' — critical policy requires override";

        return new ContractPolicy.PolicyDecision(policy.PolicyId, isAllowed, reason, DateTimeOffset.UtcNow);
    }
}
