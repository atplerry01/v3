namespace Whycespace.Systems.Midstream.WSS.Stores;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowInstanceRegistryStore
{
    void Save(WorkflowInstance instance);
    WorkflowInstance Get(string instanceId);
    IReadOnlyList<WorkflowInstance> GetAll();
    void Update(WorkflowInstance instance);
    void Remove(string instanceId);
}
