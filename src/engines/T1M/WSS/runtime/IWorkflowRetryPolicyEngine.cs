namespace Whycespace.Engines.T1M.WSS.Runtime;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowRetryPolicyEngine
{
    RetryDecision EvaluateRetryPolicy(WorkflowFailurePolicy policy, int currentRetryCount);
    void RegisterRetryAttempt(string instanceId, string stepId);
    int GetRetryCount(string instanceId, string stepId);
    void ResetRetryCount(string instanceId, string stepId);
}
