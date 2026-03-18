using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Spv.Governance.Policy;

public sealed class SpvPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public SpvPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateSpvCreationAsync(string clusterId, decimal allocatedCapital, Guid initiatorId)
    {
        var context = new PolicyContext(
            SubjectId: initiatorId.ToString(),
            ResourceId: clusterId,
            Action: "CreateSpv",
            Attributes: new Dictionary<string, object>
            {
                ["allocatedCapital"] = allocatedCapital
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateLifecycleTransitionAsync(Guid spvId, string fromState, string toState, Guid initiatorId)
    {
        var context = new PolicyContext(
            SubjectId: initiatorId.ToString(),
            ResourceId: spvId.ToString(),
            Action: "TransitionLifecycle",
            Attributes: new Dictionary<string, object>
            {
                ["fromState"] = fromState,
                ["toState"] = toState
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateCapitalAllocationAsync(Guid spvId, Guid investorId, decimal percentage)
    {
        var context = new PolicyContext(
            SubjectId: investorId.ToString(),
            ResourceId: spvId.ToString(),
            Action: "AllocateCapital",
            Attributes: new Dictionary<string, object>
            {
                ["allocationPercentage"] = percentage
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
