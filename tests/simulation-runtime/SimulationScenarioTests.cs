namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Loader;
using Whycespace.SimulationRuntime.Models;

public sealed class SimulationScenarioTests
{
    [Fact]
    public void Load_SeededScenario_ReturnsScenario()
    {
        var loader = new SimulationScenarioLoader();
        var scenarioId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var scenario = loader.Load(scenarioId);

        Assert.Equal("WhyceMobility", scenario.ClusterName);
        Assert.Equal(50, scenario.SpvCount);
        Assert.Equal(100_000m, scenario.CapitalPerSpv);
        Assert.Equal(5, scenario.DurationYears);
    }

    [Fact]
    public void Load_UnknownScenario_Throws()
    {
        var loader = new SimulationScenarioLoader();

        Assert.Throws<KeyNotFoundException>(() => loader.Load(Guid.NewGuid()));
    }

    [Fact]
    public void Register_CustomScenario_IsLoadable()
    {
        var loader = new SimulationScenarioLoader();
        var id = Guid.NewGuid();
        var scenario = new SimulationScenario(id, "TestCluster", 10, 50_000m, 3);

        loader.Register(scenario);

        var loaded = loader.Load(id);
        Assert.Equal("TestCluster", loaded.ClusterName);
    }
}
