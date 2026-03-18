using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Work.Shared.Policy;

public sealed class WorkPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public WorkPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateTaskAssignmentAsync(string workerId, string taskType, string clusterId)
    {
        var context = new PolicyContext(
            SubjectId: workerId,
            ResourceId: clusterId,
            Action: "AssignTask",
            Attributes: new Dictionary<string, object>
            {
                ["taskType"] = taskType,
                ["clusterId"] = clusterId
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateWorkExecutionAsync(string workerId, string clusterId, string subClusterId)
    {
        var context = new PolicyContext(
            SubjectId: workerId,
            ResourceId: $"{clusterId}/{subClusterId}",
            Action: "ExecuteWork",
            Attributes: new Dictionary<string, object>
            {
                ["clusterId"] = clusterId,
                ["subClusterId"] = subClusterId
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
