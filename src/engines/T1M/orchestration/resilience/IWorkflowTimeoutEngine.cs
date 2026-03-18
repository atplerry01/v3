namespace Whycespace.Engines.T1M.Orchestration.Resilience;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowTimeoutEngine
{
    void RegisterStepTimeout(string instanceId, string stepId, TimeSpan timeout);
    void RegisterWorkflowTimeout(string instanceId, TimeSpan timeout);
    TimeoutDecision CheckStepTimeout(string instanceId, string stepId);
    TimeoutDecision CheckWorkflowTimeout(string instanceId);
    void ClearTimeout(string instanceId, string stepId);
}
