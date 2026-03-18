namespace Whycespace.Systems.Midstream.WSS.Governance;

using Whycespace.Contracts.Policy;

public sealed class WorkflowPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public WorkflowPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateWorkflowStartAsync(
        string workflowName, string initiatorId, string clusterId)
    {
        var context = new PolicyContext(
            initiatorId,
            workflowName,
            "StartWorkflow",
            new Dictionary<string, object>
            {
                ["clusterId"] = clusterId,
                ["workflowName"] = workflowName
            });

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateStepExecutionAsync(
        string workflowId, string stepId, string engineName)
    {
        var context = new PolicyContext(
            workflowId,
            stepId,
            "ExecuteStep",
            new Dictionary<string, object>
            {
                ["engineName"] = engineName,
                ["stepId"] = stepId
            });

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
