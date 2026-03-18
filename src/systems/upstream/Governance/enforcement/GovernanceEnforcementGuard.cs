namespace Whycespace.Systems.Upstream.Governance.Enforcement;

using Whycespace.Contracts.Policy;
using Whycespace.Systems.Upstream.Governance.Policy;

public sealed class GovernanceEnforcementGuard
{
    private readonly GovernancePolicyEnforcer _enforcer;

    public GovernanceEnforcementGuard(GovernancePolicyEnforcer enforcer)
    {
        _enforcer = enforcer;
    }

    public async Task<PolicyEvaluationResult> GuardProposalCreation(
        string actorId, string proposalType, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceProposalCreation(actorId, proposalType, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardVote(
        string actorId, string proposalId, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceVoteCast(actorId, proposalId, attributes);
    }

    public async Task<PolicyEvaluationResult> GuardEmergency(
        string actorId, string severity, IReadOnlyDictionary<string, object>? metadata = null)
    {
        var attributes = metadata ?? new Dictionary<string, object>();
        return await _enforcer.EnforceEmergencyAction(actorId, severity, attributes);
    }
}
