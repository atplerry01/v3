using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Clusters.Administration.Policy;

public sealed class ClusterPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public ClusterPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateClusterActivationAsync(string clusterId, Guid initiatorId)
    {
        var context = new PolicyContext(
            SubjectId: initiatorId.ToString(),
            ResourceId: clusterId,
            Action: "ActivateCluster",
            Attributes: new Dictionary<string, object>
            {
                ["clusterId"] = clusterId
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateProviderRegistrationAsync(Guid identityId, string clusterId, string providerType)
    {
        var context = new PolicyContext(
            SubjectId: identityId.ToString(),
            ResourceId: clusterId,
            Action: "RegisterProvider",
            Attributes: new Dictionary<string, object>
            {
                ["providerType"] = providerType
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateAdministratorAssignmentAsync(Guid assignerIdentityId, Guid targetIdentityId, string clusterId, string role)
    {
        var context = new PolicyContext(
            SubjectId: assignerIdentityId.ToString(),
            ResourceId: clusterId,
            Action: "AssignAdministrator",
            Attributes: new Dictionary<string, object>
            {
                ["targetIdentityId"] = targetIdentityId.ToString(),
                ["role"] = role
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
