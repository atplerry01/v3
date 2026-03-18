
namespace Whycespace.WorkflowRuntime;

using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Shared.Envelopes;

/// <summary>
/// Contract for workflow event routing. Implemented by Whycespace.Runtime.EventFabricRuntime.Workflow.WorkflowEventRouter.
/// </summary>
public interface IWorkflowEventRouter
{
    Task PublishEvent(string eventType, string workflowId, string instanceId, IDictionary<string, object>? payload = null);
    void Subscribe(string eventType, Func<WorkflowEventEnvelope, Task> handler);
    Task RouteInternalEvent(WorkflowEventEnvelope envelope);
}
