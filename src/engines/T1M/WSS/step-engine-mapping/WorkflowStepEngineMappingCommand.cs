namespace Whycespace.Engines.T1M.WSS.StepEngineMapping;

public sealed record WorkflowStepEngineMappingCommand(
    string WorkflowId,
    IReadOnlyList<StepEngineMappingInput> WorkflowSteps
)
{
    public static WorkflowStepEngineMappingCommand FromContextData(IReadOnlyDictionary<string, object> data)
    {
        var workflowId = data.GetValueOrDefault("workflowId") as string ?? string.Empty;
        var steps = ResolveSteps(data.GetValueOrDefault("workflowSteps"));

        return new WorkflowStepEngineMappingCommand(workflowId, steps);
    }

    private static IReadOnlyList<StepEngineMappingInput> ResolveSteps(object? value)
    {
        if (value is IReadOnlyList<StepEngineMappingInput> steps)
            return steps;

        if (value is IEnumerable<object> items)
        {
            return items.OfType<IReadOnlyDictionary<string, object>>()
                .Select(d => new StepEngineMappingInput(
                    d.GetValueOrDefault("stepId") as string ?? string.Empty,
                    d.GetValueOrDefault("stepName") as string ?? string.Empty,
                    d.GetValueOrDefault("engineName") as string ?? string.Empty))
                .ToList();
        }

        return Array.Empty<StepEngineMappingInput>();
    }
}

public sealed record StepEngineMappingInput(
    string StepId,
    string StepName,
    string EngineName
);
