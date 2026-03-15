namespace Whycespace.RuntimeGovernance;

public sealed record EngineInvocationGovernanceResult(
    Guid InvocationId,
    string EngineName,
    GovernanceDecision GovernanceDecision,
    string DecisionReason,
    Guid PolicyEvaluationId,
    DateTime EvaluatedAt);

public enum GovernanceDecision
{
    Approved,
    Denied,
    Rejected
}
