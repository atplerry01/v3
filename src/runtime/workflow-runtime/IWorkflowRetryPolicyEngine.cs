namespace Whycespace.WorkflowRuntime;

/// <summary>
/// Contract for retry policy evaluation. Implemented by Whycespace.Runtime.Reliability.Retry.WorkflowRetryPolicyEngine.
/// </summary>
public interface IWorkflowRetryPolicyEngine
{
    WorkflowRetryPolicyResult EvaluateRetryPolicy(WorkflowRetryPolicyCommand command);
}
