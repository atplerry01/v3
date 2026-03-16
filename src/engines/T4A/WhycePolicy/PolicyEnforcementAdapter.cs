namespace Whycespace.Engines.T4A.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed class PolicyEnforcementAdapter
{
    public PolicyEnforcementResult Enforce(PolicyEnforcementInput input)
    {
        var decision = input.FinalDecision;
        var action = DetermineEnforcementAction(decision);
        var reason = BuildReason(decision, action);

        return new PolicyEnforcementResult(
            Allowed: action == EnforcementAction.AllowExecution,
            EnforcementAction: action,
            Reason: reason,
            EnforcedAt: DateTime.UtcNow
        );
    }

    private static EnforcementAction DetermineEnforcementAction(PolicyDecision decision)
    {
        if (!decision.Allowed)
        {
            return decision.Action switch
            {
                "escalate" => EnforcementAction.EscalateToGovernance,
                "require_guardian" => EnforcementAction.RequireGuardianApproval,
                "require_quorum" => EnforcementAction.RequireQuorumApproval,
                _ => EnforcementAction.DenyExecution
            };
        }

        return EnforcementAction.AllowExecution;
    }

    private static string BuildReason(PolicyDecision decision, EnforcementAction action)
    {
        return action switch
        {
            EnforcementAction.AllowExecution =>
                $"Execution allowed: {decision.Reason}",
            EnforcementAction.DenyExecution =>
                $"Execution denied: {decision.Reason}",
            EnforcementAction.RequireGuardianApproval =>
                $"Guardian approval required: {decision.Reason}",
            EnforcementAction.RequireQuorumApproval =>
                $"Quorum approval required: {decision.Reason}",
            EnforcementAction.EscalateToGovernance =>
                $"Governance escalation required: {decision.Reason}",
            _ => decision.Reason
        };
    }
}
