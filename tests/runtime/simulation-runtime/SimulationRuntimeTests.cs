namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Runtime;

public sealed class SimulationRuntimeTests
{
    [Fact]
    public void RunSimulation_IsDeterministic()
    {
        var engine = new SimulationRuntimeEngine();
        var scenario = new SimulationScenario(
            Guid.NewGuid(), "WhyceMobility", 50, 100_000m, 5);

        var result1 = engine.RunSimulation(scenario);
        var result2 = engine.RunSimulation(scenario);

        Assert.Equal(result1.ProjectedAssets, result2.ProjectedAssets);
        Assert.Equal(result1.ProjectedRevenue, result2.ProjectedRevenue);
        Assert.Equal(result1.ProjectedProfit, result2.ProjectedProfit);
    }

    [Fact]
    public void RunSimulation_ProducesPositiveResults()
    {
        var engine = new SimulationRuntimeEngine();
        var scenario = new SimulationScenario(
            Guid.NewGuid(), "WhyceMobility", 50, 100_000m, 5);

        var result = engine.RunSimulation(scenario);

        Assert.True(result.ProjectedAssets > 0);
        Assert.True(result.ProjectedRevenue > 0);
        Assert.True(result.ProjectedProfit > 0);
    }

    [Fact]
    public void RunSimulation_ScenarioId_IsPreserved()
    {
        var engine = new SimulationRuntimeEngine();
        var id = Guid.NewGuid();
        var scenario = new SimulationScenario(id, "Test", 10, 50_000m, 3);

        var result = engine.RunSimulation(scenario);

        Assert.Equal(id, result.ScenarioId);
    }
}
