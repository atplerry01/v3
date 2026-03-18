namespace Whycespace.Engines.T1M.Orchestration.Scheduling;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowLifecycleEngine
{
    Task<LifecycleDecision> StartWorkflow(string workflowId, string version, IDictionary<string, object>? context);
    Task<LifecycleDecision> AdvanceStep(string instanceId);
    Task<LifecycleDecision> CompleteStep(string instanceId, string stepId);
    Task<LifecycleDecision> FailStep(string instanceId, string stepId, string reason);
    Task<LifecycleDecision> CompleteWorkflow(string instanceId);
    Task<LifecycleDecision> TerminateWorkflow(string instanceId);
}
