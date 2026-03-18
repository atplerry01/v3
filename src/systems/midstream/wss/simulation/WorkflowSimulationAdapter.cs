namespace Whycespace.Systems.Midstream.WSS.Simulation;

using Whycespace.Systems.Midstream.WSS.Contracts;

public sealed class WorkflowSimulationAdapter
{
    private readonly List<SimulationResult> _results = new();

    public async Task<SimulationResult> SimulateWorkflowAsync(
        IWorkflowDefinition definition,
        IReadOnlyDictionary<string, object> context,
        bool dryRun = true)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var graph = definition.BuildGraph();
        var result = new SimulationResult(
            Guid.NewGuid().ToString(),
            definition.WorkflowName,
            graph.Steps.Count,
            dryRun,
            SimulationOutcome.Simulated,
            DateTimeOffset.UtcNow);

        _results.Add(result);
        return await Task.FromResult(result);
    }

    public IReadOnlyList<SimulationResult> GetResults() => _results;
}

public sealed record SimulationResult(
    string SimulationId,
    string WorkflowName,
    int StepCount,
    bool DryRun,
    SimulationOutcome Outcome,
    DateTimeOffset SimulatedAt
);

public enum SimulationOutcome
{
    Simulated,
    WouldSucceed,
    WouldFail,
    PolicyBlocked
}
