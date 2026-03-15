namespace Whycespace.Engines.T1M.WSS.Resolution;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

/// <summary>
/// Resolves step dependencies within a workflow execution graph.
/// Determines which steps are ready to execute, which must wait, and which can run in parallel.
/// This engine enables the WSS runtime dispatcher to schedule workflow execution safely and deterministically.
/// </summary>
[EngineManifest("WorkflowDependencyResolution", EngineTier.T1M, EngineKind.Decision,
    "WorkflowDependencyCommand", typeof(EngineEvent))]
public sealed class WorkflowDependencyResolutionEngine : IEngine
{
    public string Name => "WorkflowDependencyResolution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ExtractCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid command: missing workflowId or workflowSteps"));

        var result = ResolveDependencies(command);

        var events = BuildEvents(context, result);
        var output = BuildOutput(result);

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    /// <summary>
    /// Core dependency resolution logic. Stateless and deterministic.
    /// </summary>
    public static WorkflowDependencyResolutionResult ResolveDependencies(WorkflowDependencyCommand command)
    {
        var completedSet = new HashSet<string>(command.CompletedSteps, StringComparer.Ordinal);
        var allStepIds = new HashSet<string>(command.WorkflowSteps.Select(s => s.StepId), StringComparer.Ordinal);

        var readySteps = new List<string>();
        var blockedSteps = new List<string>();

        foreach (var step in command.WorkflowSteps)
        {
            // Skip steps that are already completed
            if (completedSet.Contains(step.StepId))
                continue;

            // A step is ready if all its dependencies are completed
            var allDependenciesMet = step.Dependencies.Count == 0
                || step.Dependencies.All(dep => completedSet.Contains(dep));

            if (allDependenciesMet)
                readySteps.Add(step.StepId);
            else
                blockedSteps.Add(step.StepId);
        }

        // Sort for deterministic output
        readySteps.Sort(StringComparer.Ordinal);
        blockedSteps.Sort(StringComparer.Ordinal);

        return new WorkflowDependencyResolutionResult(
            command.WorkflowId,
            readySteps,
            blockedSteps,
            command.CompletedSteps.ToList(),
            DateTimeOffset.UtcNow);
    }

    private static WorkflowDependencyCommand? ExtractCommand(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        if (string.IsNullOrWhiteSpace(workflowId))
            return null;

        var stepsData = context.Data.GetValueOrDefault("workflowSteps") as IReadOnlyList<DependencyStep>;
        if (stepsData is null || stepsData.Count == 0)
            return null;

        var completedSteps = context.Data.GetValueOrDefault("completedSteps") as IReadOnlyList<string>
            ?? Array.Empty<string>();

        return new WorkflowDependencyCommand(workflowId, stepsData, completedSteps);
    }

    private static IReadOnlyList<EngineEvent> BuildEvents(
        EngineContext context,
        WorkflowDependencyResolutionResult result)
    {
        var aggregateId = Guid.TryParse(context.WorkflowId, out var parsed)
            ? parsed
            : Guid.NewGuid();

        var eventType = result.ReadySteps.Count > 0
            ? "WorkflowStepDependenciesResolved"
            : "WorkflowStepDependenciesBlocked";

        return new[]
        {
            EngineEvent.Create(eventType, aggregateId, new Dictionary<string, object>
            {
                ["workflowId"] = result.WorkflowId,
                ["readyCount"] = result.ReadySteps.Count,
                ["blockedCount"] = result.BlockedSteps.Count,
                ["completedCount"] = result.CompletedSteps.Count
            })
        };
    }

    private static IReadOnlyDictionary<string, object> BuildOutput(
        WorkflowDependencyResolutionResult result)
    {
        return new Dictionary<string, object>
        {
            ["workflowId"] = result.WorkflowId,
            ["readySteps"] = result.ReadySteps,
            ["blockedSteps"] = result.BlockedSteps,
            ["completedSteps"] = result.CompletedSteps,
            ["evaluatedAt"] = result.EvaluatedAt
        };
    }
}
