namespace Whycespace.Runtime.PolicyEnforcement;

public interface IPolicyDecisionAdapter
{
    Task<PolicyDecisionResult> EvaluateAsync(PolicyEvaluationContext context, CancellationToken cancellationToken = default);
}

public sealed class DefaultPolicyDecisionAdapter : IPolicyDecisionAdapter
{
    public Task<PolicyDecisionResult> EvaluateAsync(PolicyEvaluationContext context, CancellationToken cancellationToken = default)
    {
        // Default: allow all — WhycePolicy integration point
        return Task.FromResult(PolicyDecisionResult.Allowed());
    }
}

public sealed record PolicyDecisionResult(
    bool IsAllowed,
    string? Reason = null,
    string? PolicyId = null)
{
    public static PolicyDecisionResult Allowed() => new(true);
    public static PolicyDecisionResult Denied(string reason, string? policyId = null) => new(false, reason, policyId);
}

public sealed class PolicyViolationException : Exception
{
    public PolicyViolationException(string message) : base(message) { }
}
