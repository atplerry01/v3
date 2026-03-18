namespace Whycespace.Systems.Upstream.Governance.Policy;

using Whycespace.Contracts.Policy;

public sealed class GovernancePolicyEnforcer
{
    private readonly IPolicyEvaluator _policyEvaluator;

    public GovernancePolicyEnforcer(IPolicyEvaluator policyEvaluator)
    {
        _policyEvaluator = policyEvaluator;
    }

    public async Task<PolicyEvaluationResult> EnforceProposalCreation(
        string actorId, string proposalType, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, "governance.proposal", $"create.{proposalType}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceVoteCast(
        string actorId, string proposalId, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"governance.proposal.{proposalId}", "vote.cast", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceEmergencyAction(
        string actorId, string severity, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, "governance.emergency", $"trigger.{severity}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceDisputeRaised(
        string actorId, string proposalId, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"governance.dispute.{proposalId}", "raise", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }

    public async Task<PolicyEvaluationResult> EnforceRoleAssignment(
        string actorId, string targetId, string role, IReadOnlyDictionary<string, object> attributes)
    {
        var context = new PolicyContext(actorId, $"governance.role.{targetId}", $"assign.{role}", attributes);
        return await _policyEvaluator.EvaluateAsync(context);
    }
}
