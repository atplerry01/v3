namespace Whycespace.Systems.Midstream.WhycePlus.Simulation;

using Whycespace.Systems.Midstream.WhycePlus.Planning;

public sealed class PlanningSimulationEngine
{
    private readonly ScenarioPlanner _planner;
    private readonly List<PlanningSimulationResult> _results = new();

    public PlanningSimulationEngine(ScenarioPlanner planner)
    {
        _planner = planner;
    }

    public PlanningSimulationResult SimulateScenario(PlanningScenario scenario, bool dryRun = true)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var result = new PlanningSimulationResult(
            Guid.NewGuid().ToString(),
            scenario.ScenarioId,
            dryRun,
            PlanningSimulationOutcome.Simulated,
            "Scenario simulated successfully",
            DateTimeOffset.UtcNow);

        _results.Add(result);
        return result;
    }

    public IReadOnlyList<PlanningSimulationResult> GetResults() => _results;
}

public sealed record PlanningSimulationResult(
    string SimulationId,
    string ScenarioId,
    bool DryRun,
    PlanningSimulationOutcome Outcome,
    string Summary,
    DateTimeOffset SimulatedAt
);

public enum PlanningSimulationOutcome
{
    Simulated,
    Feasible,
    Infeasible,
    PolicyBlocked
}
