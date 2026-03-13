namespace Whycespace.Engines.T1M.WSS.Runtime;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Events;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.System.Midstream.WSS.Events;
using Whycespace.System.Midstream.WSS.Kafka;

[EngineManifest("WorkflowEventRouter", EngineTier.T1M, EngineKind.Decision, "WorkflowEventRouterRequest", typeof(EngineEvent))]
public sealed class WorkflowEventRouter : IEngine, IWorkflowEventRouter
{
    private const string KafkaTopic = "whyce.wss.workflow.events";

    private readonly KafkaEventPublisher _kafkaPublisher;
    private readonly ConcurrentDictionary<string, List<Func<WorkflowEventEnvelope, Task>>> _subscribers = new();
    private readonly object _subscriberLock = new();

    public string Name => "WorkflowEventRouter";

    public WorkflowEventRouter(KafkaEventPublisher kafkaPublisher)
    {
        _kafkaPublisher = kafkaPublisher;
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

    public async Task PublishEvent(
        string eventType,
        string workflowId,
        string instanceId,
        IDictionary<string, object>? payload = null)
    {
        var envelope = WorkflowEventEnvelope.Create(eventType, workflowId, instanceId, payload);

        var systemEvent = SystemEvent.Create(
            eventType,
            Guid.TryParse(workflowId, out var aggregateId) ? aggregateId : Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["workflowId"] = workflowId,
                ["instanceId"] = instanceId,
                ["eventId"] = envelope.EventId.ToString(),
                ["timestamp"] = envelope.Timestamp.ToString("O")
            });

        await _kafkaPublisher.PublishToTopicAsync(KafkaTopic, systemEvent);
        await RouteInternalEvent(envelope);
    }

    public void Subscribe(string eventType, Func<WorkflowEventEnvelope, Task> handler)
    {
        lock (_subscriberLock)
        {
            var handlers = _subscribers.GetOrAdd(eventType, _ => new List<Func<WorkflowEventEnvelope, Task>>());
            handlers.Add(handler);
        }
    }

    public async Task RouteInternalEvent(WorkflowEventEnvelope envelope)
    {
        List<Func<WorkflowEventEnvelope, Task>>? handlers;

        lock (_subscriberLock)
        {
            if (!_subscribers.TryGetValue(envelope.EventType, out var list))
                return;

            handlers = new List<Func<WorkflowEventEnvelope, Task>>(list);
        }

        foreach (var handler in handlers)
        {
            await handler(envelope);
        }
    }

    private async Task<EngineResult> HandlePublishAction(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string ?? context.WorkflowId;
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? context.InvocationId.ToString();

        if (string.IsNullOrWhiteSpace(eventType))
            return EngineResult.Fail("Missing eventType");

        var payload = context.Data
            .Where(kv => kv.Key is not "action" and not "eventType" and not "workflowId" and not "instanceId")
            .ToDictionary(kv => kv.Key, kv => kv.Value) as IDictionary<string, object>;

        await PublishEvent(eventType, workflowId, instanceId, payload);

        var events = new[]
        {
            EngineEvent.Create("WorkflowEventPublished", Guid.Parse(workflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["workflowId"] = workflowId,
                    ["instanceId"] = instanceId
                })
        };

        return EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["workflowId"] = workflowId,
            ["instanceId"] = instanceId,
            ["published"] = true
        });
    }

    private Task<EngineResult> HandleSubscribeAction(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;

        if (string.IsNullOrWhiteSpace(eventType))
            return Task.FromResult(EngineResult.Fail("Missing eventType"));

        var events = new[]
        {
            EngineEvent.Create("WorkflowEventSubscribed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["subscribed"] = true
        }));
    }

    private async Task<EngineResult> HandleRouteAction(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string ?? context.WorkflowId;
        var instanceId = context.Data.GetValueOrDefault("instanceId") as string ?? context.InvocationId.ToString();

        if (string.IsNullOrWhiteSpace(eventType))
            return EngineResult.Fail("Missing eventType");

        var envelope = WorkflowEventEnvelope.Create(eventType, workflowId, instanceId,
            context.Data.ToDictionary(kv => kv.Key, kv => kv.Value));

        await RouteInternalEvent(envelope);

        int handlerCount;
        lock (_subscriberLock)
        {
            handlerCount = _subscribers.TryGetValue(eventType, out var list) ? list.Count : 0;
        }

        var events = new[]
        {
            EngineEvent.Create("WorkflowEventRouted", Guid.Parse(workflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["handlerCount"] = handlerCount
                })
        };

        return EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["routed"] = true,
            ["handlerCount"] = handlerCount
        });
    }
}
