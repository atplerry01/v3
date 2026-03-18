namespace Whycespace.Systems.Midstream.WhycePlus.Planning;

using Whycespace.Contracts.Policy;

public sealed class WhycePlusPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public WhycePlusPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluatePlanningDirectiveAsync(
        string directiveId, string scope, string initiatorId)
    {
        var context = new PolicyContext(
            initiatorId,
            directiveId,
            "IssuePlanningDirective",
            new Dictionary<string, object>
            {
                ["scope"] = scope,
                ["directiveId"] = directiveId
            });

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateScenarioExecutionAsync(
        string scenarioId, string scope)
    {
        var context = new PolicyContext(
            scenarioId,
            scope,
            "ExecuteScenario",
            new Dictionary<string, object>
            {
                ["scenarioId"] = scenarioId
            });

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
