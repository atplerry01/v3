namespace Whycespace.Engines.T1M.WSS.Runtime.Retry;

/// <summary>
/// Contract for stateless retry policy evaluation.
/// Evaluates retry eligibility and calculates delays — does not manage retry state.
/// </summary>
public interface IWorkflowRetryPolicyEngine
{
    WorkflowRetryPolicyResult EvaluateRetryPolicy(WorkflowRetryPolicyCommand command);
}
