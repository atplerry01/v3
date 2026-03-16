namespace Whycespace.Runtime.EventFabricRuntime.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Systems.Midstream.WSS.Events;

[EngineManifest("WorkflowEventRouter", EngineTier.T1M, EngineKind.Decision, "WorkflowEventRouterRequest", typeof(EngineEvent))]
public sealed class WorkflowEventRouter : IEngine, Whycespace.WorkflowRuntime.IWorkflowEventRouter
{
    private const string KafkaTopic = "whyce.wss.workflow.events";
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, List<Func<WorkflowEventEnvelope, Task>>> _subscribers = new();
    private readonly object _subscriberLock = new();

    public string Name => "WorkflowEventRouter";

    public WorkflowEventRouter(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "publish" => HandlePublishAction(context),
            "subscribe" => HandleSubscribeAction(context),
            "route" => HandleRouteAction(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: publish, subscribe, route"))
        };
    }

    public async Task PublishEvent(string eventType, string workflowId, string instanceId, IDictionary<string, object>? payload = null)
    {
        var envelope = new WorkflowEventEnvelope(
            Guid.NewGuid(),
            eventType,
            workflowId,
            instanceId,
            DateTimeOffset.UtcNow,
            payload != null ? new Dictionary<string, object>(payload) : new Dictionary<string, object>());

        var systemEvent = SystemEvent.Create(eventType, Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["topic"] = KafkaTopic,
                ["workflowId"] = workflowId,
                ["instanceId"] = instanceId
            });

        await _eventBus.PublishAsync(systemEvent);
        await RouteInternalEvent(envelope);
    }

    public void Subscribe(string eventType, Func<WorkflowEventEnvelope, Task> handler)
    {
        lock (_subscriberLock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Func<WorkflowEventEnvelope, Task>>();
                _subscribers[eventType] = handlers;
            }
            handlers.Add(handler);
        }
    }

    public async Task RouteInternalEvent(WorkflowEventEnvelope envelope)
    {
        if (_subscribers.TryGetValue(envelope.EventType, out var handlers))
        {
            List<Func<WorkflowEventEnvelope, Task>> snapshot;
            lock (_subscriberLock)
            {
                snapshot = handlers.ToList();
            }

            foreach (var handler in snapshot)
            {
                await handler(envelope);
            }
        }
    }

    private async Task<EngineResult> HandlePublishAction(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string ?? "Unknown";
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string ?? context.WorkflowId;
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";

        await PublishEvent(eventType, workflowId, instanceId, new Dictionary<string, object>(context.Data));

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["published"] = true,
            ["eventType"] = eventType
        });
    }

    private Task<EngineResult> HandleSubscribeAction(EngineContext context)
    {
        return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["subscribed"] = true
        }));
    }

    private async Task<EngineResult> HandleRouteAction(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string ?? "Unknown";
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string ?? context.WorkflowId;
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? "";

        var envelope = new WorkflowEventEnvelope(
            Guid.NewGuid(),
            eventType,
            workflowId,
            instanceId,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object>(context.Data));

        await RouteInternalEvent(envelope);

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["routed"] = true,
            ["eventType"] = eventType
        });
    }
}
