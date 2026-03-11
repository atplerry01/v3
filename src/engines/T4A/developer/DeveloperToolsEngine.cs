namespace Whycespace.Engines.T4A.Developer;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("DeveloperTools", EngineTier.T4A, EngineKind.Mutation, "DeveloperToolsRequest", typeof(EngineEvent))]
public sealed class DeveloperToolsEngine : IEngine
{
    public string Name => "DeveloperTools";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var tool = context.Data.GetValueOrDefault("tool") as string;
        if (string.IsNullOrEmpty(tool))
            return Task.FromResult(EngineResult.Fail("Missing tool"));

        var environment = context.Data.GetValueOrDefault("environment") as string ?? "development";
        if (environment == "production")
            return Task.FromResult(EngineResult.Fail("Developer tools are disabled in production"));

        return tool switch
        {
            "workflow.inspect" => InspectWorkflow(context),
            "workflow.simulate" => SimulateWorkflow(context),
            "engine.test" => TestEngine(context),
            "event.replay" => ReplayEvent(context),
            "projection.query" => QueryProjection(context),
            "context.dump" => DumpContext(context),
            "pipeline.trace" => TracePipeline(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown tool: {tool}"))
        };
    }

    private static Task<EngineResult> InspectWorkflow(EngineContext context)
    {
        var targetWorkflowId = context.Data.GetValueOrDefault("targetWorkflowId") as string;
        if (string.IsNullOrEmpty(targetWorkflowId))
            return Task.FromResult(EngineResult.Fail("Missing targetWorkflowId"));

        var events = new[]
        {
            EngineEvent.Create("DevWorkflowInspected", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["targetWorkflowId"] = targetWorkflowId,
                    ["tool"] = "workflow.inspect",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["tool"] = "workflow.inspect",
                ["targetWorkflowId"] = targetWorkflowId,
                ["commandType"] = "InspectWorkflow",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> SimulateWorkflow(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string;
        if (string.IsNullOrEmpty(workflowName))
            return Task.FromResult(EngineResult.Fail("Missing workflowName"));

        var dryRun = context.Data.GetValueOrDefault("dryRun") is true or "true";
        var simulationId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("DevWorkflowSimulated", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["simulationId"] = simulationId.ToString(),
                    ["workflowName"] = workflowName,
                    ["dryRun"] = dryRun,
                    ["tool"] = "workflow.simulate",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["tool"] = "workflow.simulate",
                ["simulationId"] = simulationId.ToString(),
                ["workflowName"] = workflowName,
                ["dryRun"] = dryRun,
                ["commandType"] = "SimulateWorkflow",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> TestEngine(EngineContext context)
    {
        var engineName = context.Data.GetValueOrDefault("engineName") as string;
        if (string.IsNullOrEmpty(engineName))
            return Task.FromResult(EngineResult.Fail("Missing engineName"));

        var testPayload = new Dictionary<string, object>();
        foreach (var kvp in context.Data)
        {
            if (kvp.Key.StartsWith("test."))
                testPayload[kvp.Key[5..]] = kvp.Value;
        }

        var testId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("DevEngineTestRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["testId"] = testId.ToString(),
                    ["engineName"] = engineName,
                    ["payloadKeys"] = string.Join(",", testPayload.Keys),
                    ["tool"] = "engine.test",
                    ["topic"] = "whyce.engine.events"
                })
        };

        var output = new Dictionary<string, object>(testPayload)
        {
            ["tool"] = "engine.test",
            ["testId"] = testId.ToString(),
            ["engineName"] = engineName,
            ["commandType"] = "TestEngine",
            ["dispatched"] = true
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> ReplayEvent(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        if (string.IsNullOrEmpty(eventType))
            return Task.FromResult(EngineResult.Fail("Missing eventType"));

        var aggregateId = context.Data.GetValueOrDefault("aggregateId") as string;
        if (string.IsNullOrEmpty(aggregateId) || !Guid.TryParse(aggregateId, out var aggGuid))
            return Task.FromResult(EngineResult.Fail("Missing or invalid aggregateId"));

        var targetTopic = context.Data.GetValueOrDefault("targetTopic") as string ?? "whyce.system.events";

        var events = new[]
        {
            EngineEvent.Create("DevEventReplayRequested", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["aggregateId"] = aggregateId,
                    ["targetTopic"] = targetTopic,
                    ["tool"] = "event.replay",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["tool"] = "event.replay",
                ["eventType"] = eventType,
                ["aggregateId"] = aggregateId,
                ["targetTopic"] = targetTopic,
                ["commandType"] = "ReplayEvent",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> QueryProjection(EngineContext context)
    {
        var projectionName = context.Data.GetValueOrDefault("projectionName") as string;
        if (string.IsNullOrEmpty(projectionName))
            return Task.FromResult(EngineResult.Fail("Missing projectionName"));

        var queryFilter = context.Data.GetValueOrDefault("queryFilter") as string ?? "*";

        var events = new[]
        {
            EngineEvent.Create("DevProjectionQueried", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["projectionName"] = projectionName,
                    ["queryFilter"] = queryFilter,
                    ["tool"] = "projection.query",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["tool"] = "projection.query",
                ["projectionName"] = projectionName,
                ["queryFilter"] = queryFilter,
                ["commandType"] = "QueryProjection",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> DumpContext(EngineContext context)
    {
        var keys = context.Data.Keys.ToList();
        var summary = new Dictionary<string, object>
        {
            ["invocationId"] = context.InvocationId.ToString(),
            ["workflowId"] = context.WorkflowId,
            ["workflowStep"] = context.WorkflowStep,
            ["partitionKey"] = context.PartitionKey,
            ["dataKeyCount"] = context.Data.Count,
            ["dataKeys"] = string.Join(",", keys)
        };

        var events = new[]
        {
            EngineEvent.Create("DevContextDumped", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["keyCount"] = keys.Count,
                    ["tool"] = "context.dump",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events, summary));
    }

    private static Task<EngineResult> TracePipeline(EngineContext context)
    {
        var commandType = context.Data.GetValueOrDefault("commandType") as string;
        if (string.IsNullOrEmpty(commandType))
            return Task.FromResult(EngineResult.Fail("Missing commandType"));

        var traceId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("DevPipelineTraceStarted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["traceId"] = traceId.ToString(),
                    ["commandType"] = commandType,
                    ["tracePoints"] = "Command,Workflow,Dispatcher,Engine,Event",
                    ["tool"] = "pipeline.trace",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["tool"] = "pipeline.trace",
                ["traceId"] = traceId.ToString(),
                ["commandType"] = commandType,
                ["commandType2"] = "TracePipeline",
                ["dispatched"] = true
            }));
    }
}
