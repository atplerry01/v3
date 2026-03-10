namespace Whycespace.SimulationRuntime.Loader;

using Whycespace.SimulationRuntime.Models;

public sealed class SimulationScenarioLoader
{
    private readonly Dictionary<Guid, SimulationScenario> _scenarios = new();

    public SimulationScenarioLoader()
    {
        Seed();
    }

    public SimulationScenario Load(Guid scenarioId)
    {
        if (_scenarios.TryGetValue(scenarioId, out var scenario))
            return scenario;

        throw new KeyNotFoundException($"Scenario {scenarioId} not found.");
    }

    public IReadOnlyList<SimulationScenario> GetAll() => _scenarios.Values.ToList();

    public void Register(SimulationScenario scenario)
    {
        _scenarios[scenario.ScenarioId] = scenario;
    }

    private void Seed()
    {
        var taxi = new SimulationScenario(
            ScenarioId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            ClusterName: "WhyceMobility",
            SpvCount: 50,
            CapitalPerSpv: 100_000m,
            DurationYears: 5
        );

        var property = new SimulationScenario(
            ScenarioId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
            ClusterName: "WhyceProperty",
            SpvCount: 20,
            CapitalPerSpv: 250_000m,
            DurationYears: 10
        );

        _scenarios[taxi.ScenarioId] = taxi;
        _scenarios[property.ScenarioId] = property;
    }
}
