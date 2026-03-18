namespace Whycespace.Engines.T2E.Economic.Capital.Models;

public sealed record CapitalPolicyDecision(
    bool IsAllowed,
    string DecisionReason,
    string PolicyId,
    DateTime EvaluatedAt
);

public static class CapitalPolicyDecisionReasons
{
    public const string PolicyApproved = "POLICY_APPROVED";
    public const string InvestorLimitExceeded = "INVESTOR_LIMIT_EXCEEDED";
    public const string PoolCapReached = "POOL_CAP_REACHED";
    public const string SpvAuthorizationRequired = "SPV_AUTHORIZATION_REQUIRED";
    public const string InvalidAllocation = "INVALID_ALLOCATION";
}
