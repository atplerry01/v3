namespace Whycespace.WorkflowRuntime;

using Whycespace.Systems.Midstream.WSS.Models;

/// <summary>
/// Contract for timeout management. Implemented by Whycespace.Runtime.Reliability.Timeout.WorkflowTimeoutEngine.
/// </summary>
public interface IWorkflowTimeoutEngine
{
    void RegisterStepTimeout(string instanceId, string stepId, TimeSpan timeout);
    void RegisterWorkflowTimeout(string instanceId, TimeSpan timeout);
    TimeoutDecision CheckStepTimeout(string instanceId, string stepId);
    TimeoutDecision CheckWorkflowTimeout(string instanceId);
    void ClearTimeout(string instanceId, string stepId);
}
