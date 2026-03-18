namespace Whycespace.Engines.T1M.Shared;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowTimeoutStore
{
    void RegisterTimeout(string instanceId, string stepId, TimeoutEntry entry);
    TimeoutEntry? GetTimeout(string instanceId, string stepId);
    void RemoveTimeout(string instanceId, string stepId);
    IReadOnlyList<TimeoutEntry> ListTimeouts();
}
