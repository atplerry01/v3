namespace Whycespace.Systems.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowInstanceRegistry
{
    WorkflowInstance CreateInstance(string workflowId, string version, IDictionary<string, object>? context);
    WorkflowInstance GetInstance(string instanceId);
    IReadOnlyList<WorkflowInstance> ListInstances();
    WorkflowInstance UpdateInstanceState(string instanceId, string currentStep, WorkflowInstanceStatus status);
    void RemoveInstance(string instanceId);
}
