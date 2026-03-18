using Whycespace.Contracts.Policy;

namespace Whycespace.Systems.Downstream.Cwg.Governance.Policy;

public sealed class CwgPolicyAdapter
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public CwgPolicyAdapter(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EvaluateContributionAsync(Guid participantId, Guid vaultId, decimal amount, string contributionType)
    {
        var context = new PolicyContext(
            SubjectId: participantId.ToString(),
            ResourceId: vaultId.ToString(),
            Action: "Contribute",
            Attributes: new Dictionary<string, object>
            {
                ["amount"] = amount,
                ["contributionType"] = contributionType
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateAllocationAsync(Guid vaultId, Guid recipientId, decimal percentage)
    {
        var context = new PolicyContext(
            SubjectId: vaultId.ToString(),
            ResourceId: recipientId.ToString(),
            Action: "Allocate",
            Attributes: new Dictionary<string, object>
            {
                ["allocationPercentage"] = percentage
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EvaluateParticipantRegistrationAsync(Guid identityId, string role, string clusterId)
    {
        var context = new PolicyContext(
            SubjectId: identityId.ToString(),
            ResourceId: clusterId,
            Action: "RegisterParticipant",
            Attributes: new Dictionary<string, object>
            {
                ["role"] = role
            }
        );

        return await _policyEvaluator.EvaluateAsync(context);
    }
}
