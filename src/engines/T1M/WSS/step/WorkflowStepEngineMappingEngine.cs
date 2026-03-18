namespace Whycespace.Engines.T1M.WSS.Step;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

/// <summary>
/// Maps workflow steps to execution engines. Validates that each step references
/// exactly one engine and produces a resolved mapping structure for runtime orchestration.
/// This engine does NOT execute steps or invoke engines — it only resolves mappings.
/// </summary>
[EngineManifest(
    "WorkflowStepEngineMapping",
    EngineTier.T1M,
    EngineKind.Decision,
    "WorkflowStepEngineMappingCommand",
    typeof(EngineEvent))]
public sealed class WorkflowStepEngineMappingEngine : IEngine
{
    public string Name => "WorkflowStepEngineMapping";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string ?? "resolveMapping";

        return action switch
        {
            "resolveMapping" => Task.FromResult(HandleResolveMapping(context)),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action '{action}'."))
        };
    }

    private static EngineResult HandleResolveMapping(EngineContext context)
    {
        var command = WorkflowStepEngineMappingCommand.FromContextData(context.Data);

        // 1. Validate workflow ID
        if (string.IsNullOrWhiteSpace(command.WorkflowId))
            return EngineResult.Fail("WorkflowId must not be empty.");

        // 2. Validate step list is non-empty
        if (command.WorkflowSteps.Count == 0)
            return EngineResult.Fail("Workflow must have at least one step.");

        // 3. Validate individual steps
        var stepValidationError = ValidateSteps(command.WorkflowSteps);
        if (stepValidationError is not null)
            return EngineResult.Fail(stepValidationError);

        // 4. Validate engine mappings
        var engineMappingError = ValidateEngineMappings(command.WorkflowSteps);
        if (engineMappingError is not null)
            return EngineResult.Fail(engineMappingError);

        // 5. Construct resolved mappings
        var resolvedAt = DateTimeOffset.UtcNow;
        var mappings = command.WorkflowSteps
            .Select(s => new ResolvedStepEngineMapping(s.StepId, s.EngineName))
            .ToList();

        // 6. Produce event
        var aggregateId = Guid.TryParse(command.WorkflowId, out var parsed) ? parsed : Guid.Empty;

        var events = new[]
        {
            EngineEvent.Create("WorkflowStepEngineMappingResolved", aggregateId,
                new Dictionary<string, object>
                {
                    ["workflowId"] = command.WorkflowId,
                    ["stepCount"] = mappings.Count,
                    ["resolvedAt"] = resolvedAt.ToString("O"),
                    ["eventVersion"] = 1,
                    ["topic"] = "whyce.wss.workflow.events"
                })
        };

        // 7. Return result
        var output = new Dictionary<string, object>
        {
            ["workflowId"] = command.WorkflowId,
            ["stepCount"] = mappings.Count,
            ["resolvedAt"] = resolvedAt.ToString("O"),
            ["mappings"] = mappings.Select(m => new Dictionary<string, object>
            {
                ["stepId"] = m.StepId,
                ["engineName"] = m.EngineName
            }).ToList()
        };

        return EngineResult.Ok(events, output);
    }

    internal static string? ValidateSteps(IReadOnlyList<StepEngineMappingInput> steps)
    {
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.StepId))
                return "Every step must have a non-empty StepId.";

            if (string.IsNullOrWhiteSpace(step.StepName))
                return $"Step '{step.StepId}': StepName must not be empty.";

            if (!seenIds.Add(step.StepId))
                return $"Duplicate step ID: '{step.StepId}'.";
        }

        return null;
    }

    internal static string? ValidateEngineMappings(IReadOnlyList<StepEngineMappingInput> steps)
    {
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.EngineName))
                return $"Step '{step.StepId}': EngineName must not be empty.";
        }

        return null;
    }

    public WorkflowStepEngineMappingResult ResolveMapping(WorkflowStepEngineMappingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.WorkflowId))
            return WorkflowStepEngineMappingResult.Fail("WorkflowId must not be empty.");

        if (command.WorkflowSteps.Count == 0)
            return WorkflowStepEngineMappingResult.Fail("Workflow must have at least one step.");

        var stepValidationError = ValidateSteps(command.WorkflowSteps);
        if (stepValidationError is not null)
            return WorkflowStepEngineMappingResult.Fail(stepValidationError);

        var engineMappingError = ValidateEngineMappings(command.WorkflowSteps);
        if (engineMappingError is not null)
            return WorkflowStepEngineMappingResult.Fail(engineMappingError);

        var resolvedAt = DateTimeOffset.UtcNow;
        var mappings = command.WorkflowSteps
            .Select(s => new ResolvedStepEngineMapping(s.StepId, s.EngineName))
            .ToList();

        return WorkflowStepEngineMappingResult.Ok(command.WorkflowId, mappings, resolvedAt);
    }
}
