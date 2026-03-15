namespace Whycespace.RuntimeGovernance;

public sealed record EnginePolicyContext(
    Guid WorkflowInstanceId,
    string EngineName,
    string EngineVersion,
    string RequestedBy,
    string WorkflowStepId,
    Guid CorrelationId);

public enum PolicyDecision
{
    Allow,
    Deny,
    ConditionalAllow
}

public sealed record PolicyEvaluationResult(
    Guid PolicyEvaluationId,
    PolicyDecision Decision,
    string Reason);

public interface IEnginePolicyEvaluator
{
    PolicyEvaluationResult Evaluate(EnginePolicyContext context);
}
