namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Engine;
using Whycespace.SimulationRuntime.Models;
using Whycespace.SimulationRuntime.Builder;
using Whycespace.SimulationRuntime.Policy;

public sealed class SimulationServiceTests
{
    [Fact]
    public void Execute_ReturnsResult()
    {
        var engine = new SimulationEngineBuilder()
            .WithPolicy(new SimulationPolicy())
            .Build();

        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object>
            {
                ["clusterName"] = "WhyceMobility",
                ["spvCount"] = 50
            });

        var result = engine.Execute(command);

        Assert.True(result.Success);
        Assert.NotEmpty(result.CapturedEvents);
    }

    [Fact]
    public void Execute_WithAggregateId_IncludesInSnapshot()
    {
        var engine = new SimulationEngineBuilder()
            .WithPolicy(new SimulationPolicy())
            .Build();

        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object> { ["cluster"] = "WhyceMobility" },
            AggregateId: "cluster-1");

        var result = engine.Execute(command);

        Assert.True(result.Success);
        Assert.Contains("cluster-1", result.StateSnapshot.AffectedAggregates);
    }

    [Fact]
    public void Execute_CapturesTraces()
    {
        var engine = new SimulationEngineBuilder()
            .WithPolicy(new SimulationPolicy())
            .Build();

        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object> { ["cluster"] = "WhyceMobility" });

        var result = engine.Execute(command);

        Assert.True(result.Traces.Count >= 2);
        Assert.Equal("CommandReceived", result.Traces[0].StepName);
    }
}
