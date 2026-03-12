namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkflowInstanceRegistry", EngineTier.T1M, EngineKind.Mutation, "WorkflowInstanceRegistryRequest", typeof(EngineEvent))]
public sealed class WorkflowInstanceRegistryEngine : IEngine
{
    private readonly WorkflowInstanceStore _store = new();

    public string Name => "WorkflowInstanceRegistry";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "create" => HandleCreate(context),
            "get" => HandleGet(context),
            "list" => HandleList(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: create, get, list"))
        };
    }

    private Task<EngineResult> HandleCreate(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var currentStep = context.Data.GetValueOrDefault("currentStep") as string ?? "";

        if (string.IsNullOrWhiteSpace(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        var instance = CreateInstance(workflowId, currentStep);

        var events = new[]
        {
            EngineEvent.Create("WorkflowInstanceCreated", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["instanceId"] = instance.InstanceId.ToString(),
                    ["workflowId"] = instance.WorkflowId
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["instanceId"] = instance.InstanceId.ToString(),
            ["workflowId"] = instance.WorkflowId,
            ["currentStep"] = instance.CurrentStep,
            ["status"] = instance.Status.ToString(),
            ["startedAt"] = instance.StartedAt.ToString("O")
        }));
    }

    private Task<EngineResult> HandleGet(EngineContext context)
    {
        var instanceIdStr = context.Data.GetValueOrDefault("instanceId") as string;

        if (string.IsNullOrWhiteSpace(instanceIdStr) || !Guid.TryParse(instanceIdStr, out var instanceId))
            return Task.FromResult(EngineResult.Fail("Missing or invalid instanceId"));

        var instance = GetInstance(instanceId);

        if (instance is null)
            return Task.FromResult(EngineResult.Fail($"Instance '{instanceId}' not found"));

        return Task.FromResult(EngineResult.Ok(
            Array.Empty<EngineEvent>(),
            new Dictionary<string, object>
            {
                ["instanceId"] = instance.InstanceId.ToString(),
                ["workflowId"] = instance.WorkflowId,
                ["currentStep"] = instance.CurrentStep,
                ["status"] = instance.Status.ToString(),
                ["startedAt"] = instance.StartedAt.ToString("O")
            }));
    }

    private Task<EngineResult> HandleList(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;

        var instances = string.IsNullOrWhiteSpace(workflowId)
            ? ListInstances()
            : _store.ListByWorkflow(workflowId);

        var items = instances.Select(i => (object)new Dictionary<string, object>
        {
            ["instanceId"] = i.InstanceId.ToString(),
            ["workflowId"] = i.WorkflowId,
            ["currentStep"] = i.CurrentStep,
            ["status"] = i.Status.ToString(),
            ["startedAt"] = i.StartedAt.ToString("O")
        }).ToList();

        return Task.FromResult(EngineResult.Ok(
            Array.Empty<EngineEvent>(),
            new Dictionary<string, object>
            {
                ["count"] = instances.Count,
                ["instances"] = items
            }));
    }

    internal WorkflowInstanceEntry CreateInstance(string workflowId, string currentStep)
    {
        var instance = new WorkflowInstanceEntry(
            Guid.NewGuid(),
            workflowId,
            currentStep,
            WorkflowStatus.Pending,
            DateTimeOffset.UtcNow
        );

        _store.Save(instance);
        return instance;
    }

    internal WorkflowInstanceEntry? GetInstance(Guid instanceId)
    {
        return _store.Get(instanceId);
    }

    internal IReadOnlyList<WorkflowInstanceEntry> ListInstances()
    {
        return _store.List();
    }
}
