namespace Whycespace.Engines.T1M.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowInstanceRegistry
{
    WorkflowInstance CreateInstance(string workflowId, string version, IDictionary<string, object>? context);
    WorkflowInstance GetInstance(string instanceId);
    IReadOnlyList<WorkflowInstance> ListInstances();
    WorkflowInstance UpdateInstanceState(string instanceId, string currentStep, WorkflowInstanceStatus status);
    void RemoveInstance(string instanceId);
}
