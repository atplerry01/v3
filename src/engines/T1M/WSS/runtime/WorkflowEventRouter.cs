namespace Whycespace.Engines.T1M.WSS.Runtime;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Stores;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkflowEventRouter", EngineTier.T1M, EngineKind.Decision, "WorkflowEventRouterRequest", typeof(EngineEvent))]
public sealed class WorkflowEventRouter : IEngine
{
    private readonly WorkflowInstanceStore _instanceStore;
    private readonly WorkflowStateStore _stateStore;
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _eventSubscriptions = new();

    public string Name => "WorkflowEventRouter";

    public WorkflowEventRouter(WorkflowInstanceStore instanceStore, WorkflowStateStore stateStore)
    {
        _instanceStore = instanceStore;
        _stateStore = stateStore;
    }

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "route" => HandleRoute(context),
            "subscribe" => HandleSubscribe(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: route, subscribe"))
        };
    }

    private Task<EngineResult> HandleRoute(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        var nextNode = context.Data.GetValueOrDefault("nextNode") as string;

        if (string.IsNullOrWhiteSpace(eventType))
            return Task.FromResult(EngineResult.Fail("Missing eventType"));

        var instances = RouteEvent(eventType);

        if (instances.Count == 0)
        {
            return Task.FromResult(EngineResult.Ok(
                Array.Empty<EngineEvent>(),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["routedCount"] = 0
                }));
        }

        var advanced = new List<Dictionary<string, object>>();
        foreach (var instanceId in instances)
        {
            var instance = _instanceStore.Get(instanceId);
            if (instance is null) continue;

            var targetNode = nextNode ?? eventType;
            var state = AdvanceWorkflow(instanceId, targetNode, context.Data);

            advanced.Add(new Dictionary<string, object>
            {
                ["instanceId"] = instanceId.ToString(),
                ["workflowId"] = instance.WorkflowId,
                ["advancedTo"] = state.CurrentNode
            });
        }

        var events = new[]
        {
            EngineEvent.Create("WorkflowEventRouted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["routedCount"] = advanced.Count
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["routedCount"] = advanced.Count,
            ["instances"] = advanced.Cast<object>().ToList()
        }));
    }

    private Task<EngineResult> HandleSubscribe(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        var instanceIdStr = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(eventType))
            return Task.FromResult(EngineResult.Fail("Missing eventType"));

        if (string.IsNullOrWhiteSpace(instanceIdStr) || !Guid.TryParse(instanceIdStr, out var instanceId))
            return Task.FromResult(EngineResult.Fail("Missing or invalid instanceId"));

        var subs = _eventSubscriptions.GetOrAdd(eventType, _ => new HashSet<Guid>());
        lock (subs)
        {
            subs.Add(instanceId);
        }

        var events = new[]
        {
            EngineEvent.Create("WorkflowEventSubscribed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["instanceId"] = instanceId.ToString()
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["instanceId"] = instanceId.ToString()
        }));
    }

    internal IReadOnlyList<Guid> RouteEvent(string eventType)
    {
        var instances = FindWorkflowInstances(eventType);
        return instances;
    }

    internal IReadOnlyList<Guid> FindWorkflowInstances(string eventType)
    {
        if (!_eventSubscriptions.TryGetValue(eventType, out var subs))
            return Array.Empty<Guid>();

        lock (subs)
        {
            return subs.ToList();
        }
    }

    internal WorkflowRuntimeState AdvanceWorkflow(Guid instanceId, string nextNode, IReadOnlyDictionary<string, object>? contextData = null)
    {
        return _stateStore.Update(instanceId, nextNode, contextData);
    }
}
