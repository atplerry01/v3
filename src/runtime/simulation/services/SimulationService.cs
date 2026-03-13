namespace Whycespace.SimulationRuntime.Services;

using Whycespace.SimulationRuntime.Loader;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Runtime;

public sealed class SimulationService
{
    private readonly SimulationScenarioLoader _loader;
    private readonly SimulationRuntimeEngine _engine;
    private readonly Dictionary<Guid, SimulationResult> _results = new();

    public SimulationService(SimulationScenarioLoader loader, SimulationRuntimeEngine engine)
    {
        _loader = loader;
        _engine = engine;
    }

    public SimulationResult RunScenario(Guid scenarioId)
    {
        var scenario = _loader.Load(scenarioId);
        var result = _engine.RunSimulation(scenario);
        _results[scenarioId] = result;
        return result;
    }

    public SimulationResult RunClusterForecast(string clusterName)
    {
        var scenario = _loader.GetAll()
            .FirstOrDefault(s => s.ClusterName == clusterName)
            ?? throw new KeyNotFoundException($"No scenario found for cluster '{clusterName}'.");

        return RunScenario(scenario.ScenarioId);
    }

    public IReadOnlyList<SimulationScenario> GetScenarios() => _loader.GetAll();

    public IReadOnlyList<SimulationResult> GetResults() => _results.Values.ToList();
}
