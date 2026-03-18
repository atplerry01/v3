namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

public sealed record PolicyAuditInput(
    string PolicyId,
    string PolicyDecision,
    string EvaluationContext,
    string EnforcementResult,
    string ActorId,
    PolicyAuditActionType ActionType
);

public enum PolicyAuditActionType
{
    POLICY_CREATED,
    POLICY_UPDATED,
    POLICY_EVALUATED,
    POLICY_ENFORCED,
    POLICY_APPROVED,
    POLICY_ACTIVATED,
    POLICY_SUSPENDED,
    POLICY_REVOKED,
    POLICY_SIMULATED,
    POLICY_FORECASTED,
    POLICY_ESCALATED
}
