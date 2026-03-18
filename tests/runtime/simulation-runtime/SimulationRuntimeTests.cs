namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Engine;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Policy;

public sealed class SimulationRuntimeTests
{
    [Fact]
    public void Execute_IsDeterministic()
    {
        var engine = new SimulationEngine(new SimulationPolicy());
        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object>
            {
                ["clusterName"] = "WhyceMobility",
                ["spvCount"] = 50,
                ["capitalPerSpv"] = 100_000m,
                ["durationYears"] = 5
            });

        var result1 = engine.Execute(command);
        var result2 = engine.Execute(command);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.CapturedEvents.Count, result2.CapturedEvents.Count);
    }

    [Fact]
    public void Execute_ProducesSuccessResult()
    {
        var engine = new SimulationEngine(new SimulationPolicy());
        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object>
            {
                ["clusterName"] = "WhyceMobility",
                ["spvCount"] = 50,
                ["capitalPerSpv"] = 100_000m,
                ["durationYears"] = 5
            });

        var result = engine.Execute(command);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.SimulationId);
        Assert.NotEmpty(result.CapturedEvents);
        Assert.NotEmpty(result.Traces);
    }

    [Fact]
    public void Execute_CommandType_IsPreservedInResult()
    {
        var engine = new SimulationEngine(new SimulationPolicy());
        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object> { ["cluster"] = "Test" });

        var result = engine.Execute(command);

        Assert.Equal("RunForecast", result.Command.CommandType);
    }
}
