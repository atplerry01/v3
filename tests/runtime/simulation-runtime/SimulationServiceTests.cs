namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Loader;
using Whycespace.SimulationRuntime.Runtime;
using Whycespace.SimulationRuntime.Services;

public sealed class SimulationServiceTests
{
    [Fact]
    public void RunScenario_ReturnsResult()
    {
        var loader = new SimulationScenarioLoader();
        var engine = new SimulationRuntimeEngine();
        var service = new SimulationService(loader, engine);
        var scenarioId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var result = service.RunScenario(scenarioId);

        Assert.Equal(scenarioId, result.ScenarioId);
        Assert.True(result.ProjectedRevenue > 0);
    }

    [Fact]
    public void RunClusterForecast_ReturnsResult()
    {
        var loader = new SimulationScenarioLoader();
        var engine = new SimulationRuntimeEngine();
        var service = new SimulationService(loader, engine);

        var result = service.RunClusterForecast("WhyceMobility");

        Assert.True(result.ProjectedRevenue > 0);
    }

    [Fact]
    public void GetResults_AfterRun_ContainsResult()
    {
        var loader = new SimulationScenarioLoader();
        var engine = new SimulationRuntimeEngine();
        var service = new SimulationService(loader, engine);
        var scenarioId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        service.RunScenario(scenarioId);

        var results = service.GetResults();
        Assert.Single(results);
        Assert.Equal(scenarioId, results[0].ScenarioId);
    }
}
