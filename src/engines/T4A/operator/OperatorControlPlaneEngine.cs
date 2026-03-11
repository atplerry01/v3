namespace Whycespace.Engines.T4A.Operator;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("OperatorControlPlane", EngineTier.T4A, EngineKind.Mutation, "OperatorControlPlaneRequest", typeof(EngineEvent))]
public sealed class OperatorControlPlaneEngine : IEngine
{
    public string Name => "OperatorControlPlane";

    private static readonly IReadOnlySet<string> AdminOperations = new HashSet<string>
    {
        // Cluster management
        "cluster.list",
        "cluster.inspect",
        "cluster.suspend",
        "cluster.resume",
        "cluster.register",

        // Engine management
        "engine.list",
        "engine.inspect",
        "engine.health",

        // Workflow management
        "workflow.list",
        "workflow.inspect",
        "workflow.cancel",
        "workflow.retry",

        // System management
        "system.health",
        "system.config",
        "system.metrics",

        // Dead letter management
        "dlq.list",
        "dlq.replay",
        "dlq.purge",

        // SPV management
        "spv.list",
        "spv.inspect",
        "spv.suspend",
        "spv.dissolve"
    };

    private static readonly IReadOnlySet<string> RequiresSystemAdmin = new HashSet<string>
    {
        "cluster.suspend", "cluster.resume", "cluster.register",
        "workflow.cancel", "system.config",
        "dlq.purge", "spv.suspend", "spv.dissolve"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string;
        if (string.IsNullOrEmpty(operation))
            return Task.FromResult(EngineResult.Fail("Missing operation"));

        var operatorId = context.Data.GetValueOrDefault("operatorId") as string;
        if (string.IsNullOrEmpty(operatorId))
            return Task.FromResult(EngineResult.Fail("Missing operatorId"));

        var operatorRole = context.Data.GetValueOrDefault("operatorRole") as string ?? "viewer";

        if (!AdminOperations.Contains(operation))
            return Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"));

        if (RequiresSystemAdmin.Contains(operation) && operatorRole != "SystemAdmin")
        {
            var deniedEvents = new[]
            {
                EngineEvent.Create("OperatorAccessDenied", Guid.Parse(context.WorkflowId),
                    new Dictionary<string, object>
                    {
                        ["operatorId"] = operatorId,
                        ["operation"] = operation,
                        ["operatorRole"] = operatorRole,
                        ["requiredRole"] = "SystemAdmin",
                        ["topic"] = "whyce.system.events"
                    })
            };

            return Task.FromResult(EngineResult.Ok(deniedEvents,
                new Dictionary<string, object>
                {
                    ["authorized"] = false,
                    ["reason"] = $"Operation '{operation}' requires SystemAdmin role"
                }));
        }

        return operation switch
        {
            "cluster.list" or "cluster.inspect" => HandleClusterOperation(operation, context),
            "engine.list" or "engine.inspect" or "engine.health" => HandleEngineOperation(operation, context),
            "workflow.list" or "workflow.inspect" or "workflow.cancel" or "workflow.retry" => HandleWorkflowOperation(operation, context),
            "system.health" or "system.config" or "system.metrics" => HandleSystemOperation(operation, context),
            "dlq.list" or "dlq.replay" or "dlq.purge" => HandleDlqOperation(operation, context),
            _ => HandleGenericOperation(operation, operatorId, context)
        };
    }

    private static Task<EngineResult> HandleClusterOperation(string operation, EngineContext context)
    {
        var clusterId = context.Data.GetValueOrDefault("clusterId") as string;
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string ?? "";

        var events = new[]
        {
            EngineEvent.Create("OperatorClusterCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["clusterId"] = clusterId ?? "all",
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["commandType"] = $"Cluster.{OperationVerb(operation)}",
                ["clusterId"] = clusterId ?? "all",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleEngineOperation(string operation, EngineContext context)
    {
        var engineName = context.Data.GetValueOrDefault("engineName") as string;
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string ?? "";

        var events = new[]
        {
            EngineEvent.Create("OperatorEngineCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["engineName"] = engineName ?? "all",
                    ["topic"] = "whyce.engine.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["commandType"] = $"Engine.{OperationVerb(operation)}",
                ["engineName"] = engineName ?? "all",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleWorkflowOperation(string operation, EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("targetWorkflowId") as string;
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string ?? "";

        if ((operation == "workflow.cancel" || operation == "workflow.retry") && string.IsNullOrEmpty(workflowId))
            return Task.FromResult(EngineResult.Fail($"Operation '{operation}' requires targetWorkflowId"));

        var events = new[]
        {
            EngineEvent.Create("OperatorWorkflowCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["targetWorkflowId"] = workflowId ?? "all",
                    ["topic"] = "whyce.workflow.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["commandType"] = $"Workflow.{OperationVerb(operation)}",
                ["targetWorkflowId"] = workflowId ?? "all",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleSystemOperation(string operation, EngineContext context)
    {
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string ?? "";

        var events = new[]
        {
            EngineEvent.Create("OperatorSystemCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["commandType"] = $"System.{OperationVerb(operation)}",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleDlqOperation(string operation, EngineContext context)
    {
        var operatorId = context.Data.GetValueOrDefault("operatorId") as string ?? "";
        var entryId = context.Data.GetValueOrDefault("entryId") as string;

        if (operation == "dlq.replay" && string.IsNullOrEmpty(entryId))
            return Task.FromResult(EngineResult.Fail("Operation 'dlq.replay' requires entryId"));

        var events = new[]
        {
            EngineEvent.Create("OperatorDlqCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["entryId"] = entryId ?? "all",
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["commandType"] = $"DLQ.{OperationVerb(operation)}",
                ["entryId"] = entryId ?? "all",
                ["dispatched"] = true
            }));
    }

    private static Task<EngineResult> HandleGenericOperation(string operation, string operatorId, EngineContext context)
    {
        var events = new[]
        {
            EngineEvent.Create("OperatorGenericCommand", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["operation"] = operation,
                    ["operatorId"] = operatorId,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["dispatched"] = true
            }));
    }

    private static string OperationVerb(string operation)
    {
        var dotIndex = operation.IndexOf('.');
        return dotIndex >= 0 && dotIndex < operation.Length - 1
            ? char.ToUpperInvariant(operation[dotIndex + 1]) + operation[(dotIndex + 2)..]
            : operation;
    }
}
