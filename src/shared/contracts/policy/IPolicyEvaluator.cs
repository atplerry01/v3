namespace Whycespace.Contracts.Policy;

public interface IPolicyEvaluator
{
    Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context);
}
