namespace Whycespace.WorkflowRuntime;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowLifecycleEngine
{
    Task<LifecycleDecision> StartWorkflow(string workflowId, string version, IDictionary<string, object>? context);
    Task<LifecycleDecision> AdvanceStep(string instanceId);
    Task<LifecycleDecision> CompleteStep(string instanceId, string stepId);
    Task<LifecycleDecision> FailStep(string instanceId, string stepId, string reason);
    Task<LifecycleDecision> CompleteWorkflow(string instanceId);
    Task<LifecycleDecision> TerminateWorkflow(string instanceId);
}
