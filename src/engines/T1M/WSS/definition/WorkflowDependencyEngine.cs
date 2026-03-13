namespace Whycespace.Engines.T1M.WSS.Definition;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkflowDependency", EngineTier.T1M, EngineKind.Decision, "WorkflowDependencyRequest", typeof(EngineEvent))]
public sealed class WorkflowDependencyEngine : IEngine
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _dependencies = new();

    public string Name => "WorkflowDependency";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "add" => HandleAdd(context),
            "get" => HandleGet(context),
            "validate" => HandleValidate(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'. Expected: add, get, validate"))
        };
    }

    private Task<EngineResult> HandleAdd(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var dependsOn = context.Data.GetValueOrDefault("dependsOn") as string;

        if (string.IsNullOrWhiteSpace(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        if (string.IsNullOrWhiteSpace(dependsOn))
            return Task.FromResult(EngineResult.Fail("Missing dependsOn"));

        if (workflowId == dependsOn)
            return Task.FromResult(EngineResult.Fail("A workflow cannot depend on itself"));

        AddDependency(workflowId, dependsOn);

        var events = new[]
        {
            EngineEvent.Create("WorkflowDependencyAdded", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["workflowId"] = workflowId,
                    ["dependsOn"] = dependsOn
                })
        };

        return Task.FromResult(EngineResult.Ok(events, new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["dependsOn"] = dependsOn
        }));
    }

    private Task<EngineResult> HandleGet(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;

        if (string.IsNullOrWhiteSpace(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        var deps = GetDependencies(workflowId);

        return Task.FromResult(EngineResult.Ok(
            Array.Empty<EngineEvent>(),
            new Dictionary<string, object>
            {
                ["workflowId"] = workflowId,
                ["dependencies"] = deps
            }));
    }

    private Task<EngineResult> HandleValidate(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var completedWorkflows = context.Data.GetValueOrDefault("completedWorkflows") as IReadOnlyList<string>;

        if (string.IsNullOrWhiteSpace(workflowId))
            return Task.FromResult(EngineResult.Fail("Missing workflowId"));

        var violations = ValidateDependencies(workflowId, completedWorkflows ?? Array.Empty<string>());

        if (violations.Count > 0)
        {
            var events = new[]
            {
                EngineEvent.Create("WorkflowDependenciesNotMet", Guid.Parse(context.WorkflowId),
                    new Dictionary<string, object>
                    {
                        ["workflowId"] = workflowId,
                        ["unmetCount"] = violations.Count
                    })
            };

            return Task.FromResult(new EngineResult(false, events, new Dictionary<string, object>
            {
                ["workflowId"] = workflowId,
                ["unmetDependencies"] = violations,
                ["canExecute"] = false
            }));
        }

        var successEvents = new[]
        {
            EngineEvent.Create("WorkflowDependenciesMet", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["workflowId"] = workflowId })
        };

        return Task.FromResult(EngineResult.Ok(successEvents, new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["canExecute"] = true
        }));
    }

    internal void AddDependency(string workflowId, string dependsOn)
    {
        var deps = _dependencies.GetOrAdd(workflowId, _ => new HashSet<string>());
        lock (deps)
        {
            deps.Add(dependsOn);
        }
    }

    internal IReadOnlyList<string> GetDependencies(string workflowId)
    {
        if (!_dependencies.TryGetValue(workflowId, out var deps))
            return Array.Empty<string>();

        lock (deps)
        {
            return deps.ToList();
        }
    }

    internal IReadOnlyList<string> ValidateDependencies(string workflowId, IReadOnlyList<string> completedWorkflows)
    {
        var deps = GetDependencies(workflowId);
        if (deps.Count == 0) return Array.Empty<string>();

        var completed = new HashSet<string>(completedWorkflows);
        return deps.Where(d => !completed.Contains(d)).ToList();
    }
}
