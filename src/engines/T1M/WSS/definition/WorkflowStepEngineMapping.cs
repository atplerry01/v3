namespace Whycespace.Engines.T1M.WSS.Definition;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkflowStepEngineMapping", EngineTier.T1M, EngineKind.Decision, "WorkflowStepEngineMappingRequest", typeof(EngineEvent))]
public sealed class WorkflowStepEngineMapping : IEngine
{
    private readonly ConcurrentDictionary<string, WorkflowStepMapping> _mappings = new();

    public string Name => "WorkflowStepEngineMapping";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "map" => HandleMap(context),
            "get" => HandleGet(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: map, get"))
        };
    }

    private Task<EngineResult> HandleMap(EngineContext context)
    {
        var stepId = context.Data.GetValueOrDefault("stepId") as string;
        var engineName = context.Data.GetValueOrDefault("engineName") as string;
        var commandName = context.Data.GetValueOrDefault("commandName") as string;

        if (string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing stepId"));

        if (string.IsNullOrWhiteSpace(engineName))
            return Task.FromResult(EngineResult.Fail("Missing engineName"));

        if (string.IsNullOrWhiteSpace(commandName))
            return Task.FromResult(EngineResult.Fail("Missing commandName"));

        var mapping = MapStep(stepId, engineName, commandName);

        var events = new[]
        {
            EngineEvent.Create("WorkflowStepMapped", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["stepId"] = mapping.StepId,
                    ["engineName"] = mapping.EngineName,
                    ["commandName"] = mapping.CommandName
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["stepId"] = mapping.StepId,
            ["engineName"] = mapping.EngineName,
            ["commandName"] = mapping.CommandName
        }));
    }

    private Task<EngineResult> HandleGet(EngineContext context)
    {
        var stepId = context.Data.GetValueOrDefault("stepId") as string;

        if (string.IsNullOrWhiteSpace(stepId))
            return Task.FromResult(EngineResult.Fail("Missing stepId"));

        var mapping = GetStepEngine(stepId);

        if (mapping is null)
            return Task.FromResult(EngineResult.Fail($"No mapping found for step '{stepId}'"));

        return Task.FromResult(EngineResult.Ok(
            Array.Empty<EngineEvent>(),
            new Dictionary<string, object>
            {
                ["stepId"] = mapping.StepId,
                ["engineName"] = mapping.EngineName,
                ["commandName"] = mapping.CommandName
            }));
    }

    internal WorkflowStepMapping MapStep(string stepId, string engineName, string commandName)
    {
        var mapping = new WorkflowStepMapping(stepId, engineName, commandName);
        _mappings[stepId] = mapping;
        return mapping;
    }

    internal WorkflowStepMapping? GetStepEngine(string stepId)
    {
        return _mappings.TryGetValue(stepId, out var mapping) ? mapping : null;
    }
}
