namespace Whycespace.Runtime.Persistence.Workflow;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowTimeoutStore
{
    void RegisterTimeout(string instanceId, string stepId, TimeoutEntry entry);
    TimeoutEntry? GetTimeout(string instanceId, string stepId);
    void RemoveTimeout(string instanceId, string stepId);
    IReadOnlyList<TimeoutEntry> ListTimeouts();
}
