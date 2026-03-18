namespace Whycespace.Systems.Midstream.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowInstanceRegistryStore
{
    void Save(WorkflowInstance instance);
    WorkflowInstance Get(string instanceId);
    IReadOnlyList<WorkflowInstance> GetAll();
    void Update(WorkflowInstance instance);
    void Remove(string instanceId);
}
