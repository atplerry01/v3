
namespace Whycespace.Engines.T1M.Orchestration.Dispatcher;

using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Shared.Envelopes;

public interface IWorkflowEventRouter
{
    Task PublishEvent(
        string eventType,
        string workflowId,
        string instanceId,
        IDictionary<string, object>? payload = null);

    void Subscribe(
        string eventType,
        Func<WorkflowEventEnvelope, Task> handler);

    Task RouteInternalEvent(WorkflowEventEnvelope envelope);
}
