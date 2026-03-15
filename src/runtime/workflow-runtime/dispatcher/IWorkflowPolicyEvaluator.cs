namespace Whycespace.WorkflowRuntime.Dispatcher;

public interface IWorkflowPolicyEvaluator
{
    Task<PolicyEvaluationOutcome> EvaluateAsync(
        EngineInvocationContext context,
        CancellationToken cancellationToken = default);
}

public sealed record PolicyEvaluationOutcome(
    bool Allowed,
    string Reason
)
{
    public static PolicyEvaluationOutcome Allow() => new(true, "Policy evaluation passed");
    public static PolicyEvaluationOutcome Deny(string reason) => new(false, reason);
}
