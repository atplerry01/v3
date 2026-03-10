namespace Whycespace.ArchitectureGuardrails.Validation;

using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.Runtime.Registry;
using Whycespace.Shared.Workflow;

public sealed record WorkflowValidationResult(
    string WorkflowName,
    bool IsValid,
    IReadOnlyList<string> Violations
);

public sealed class WorkflowArchitectureValidator
{
    private readonly EngineRegistry _registry;

    public WorkflowArchitectureValidator(EngineRegistry registry)
    {
        _registry = registry;
    }

    public WorkflowValidationResult ValidateWorkflow(WorkflowGraph graph)
    {
        var violations = new List<string>();
        var name = graph.Name;

        // Workflows must have at least one step
        if (graph.Steps.Count == 0)
            violations.Add($"{name}: Workflow must have at least one step.");

        // Workflows must have a valid WorkflowId
        if (string.IsNullOrWhiteSpace(graph.WorkflowId))
            violations.Add($"{name}: WorkflowId must not be empty.");

        // Every step must reference a registered engine (dispatcher entrypoint rule)
        foreach (var step in graph.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.EngineName))
            {
                violations.Add($"{name}/{step.StepId}: Step must reference an engine. [{ArchitectureRules.DispatcherOnlyEntrypoint}]");
                continue;
            }

            var engine = _registry.Resolve(step.EngineName);
            if (engine is null)
            {
                violations.Add($"{name}/{step.StepId}: Engine '{step.EngineName}' is not registered in the dispatcher. [{ArchitectureRules.DispatcherOnlyEntrypoint}]");
            }
        }

        // Step IDs must be unique within a workflow
        var duplicateSteps = graph.Steps
            .GroupBy(s => s.StepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateSteps.Count > 0)
            violations.Add($"{name}: Duplicate step IDs: {string.Join(", ", duplicateSteps)}.");

        // NextSteps must reference valid step IDs within the workflow
        var validStepIds = new HashSet<string>(graph.Steps.Select(s => s.StepId));
        foreach (var step in graph.Steps)
        {
            foreach (var nextStep in step.NextSteps)
            {
                if (!validStepIds.Contains(nextStep))
                    violations.Add($"{name}/{step.StepId}: NextStep '{nextStep}' does not reference a valid step ID.");
            }
        }

        return new WorkflowValidationResult(name, violations.Count == 0, violations);
    }
}
