namespace Whycespace.SimulationRuntime.Tests;

using Whycespace.SimulationRuntime.Models;

public sealed class SimulationScenarioTests
{
    [Fact]
    public void SimulationCommand_CanBeCreated_WithPayload()
    {
        var command = new SimulationCommand(
            "RunForecast",
            new Dictionary<string, object>
            {
                ["clusterName"] = "WhyceMobility",
                ["spvCount"] = 50,
                ["capitalPerSpv"] = 100_000m,
                ["durationYears"] = 5
            });

        Assert.Equal("RunForecast", command.CommandType);
        Assert.Equal("WhyceMobility", command.Payload["clusterName"]);
        Assert.Equal(50, command.Payload["spvCount"]);
        Assert.Equal(100_000m, command.Payload["capitalPerSpv"]);
        Assert.Equal(5, command.Payload["durationYears"]);
    }

    [Fact]
    public void SimulationCommand_OptionalFields_DefaultToNull()
    {
        var command = new SimulationCommand(
            "TestCommand",
            new Dictionary<string, object>());

        Assert.Null(command.AggregateId);
        Assert.Null(command.CorrelationId);
    }

    [Fact]
    public void SimulationCommand_WithAggregateId_IsAccessible()
    {
        var command = new SimulationCommand(
            "TestCommand",
            new Dictionary<string, object> { ["key"] = "value" },
            AggregateId: "agg-1",
            CorrelationId: "corr-1");

        Assert.Equal("agg-1", command.AggregateId);
        Assert.Equal("corr-1", command.CorrelationId);
    }
}
