namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.System.Spv.Engines;
using Whycespace.Contracts.Engines;

public sealed class SpvCreationEngineTests
{
    private readonly SpvCreationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateSpv",
            "partition-1", new Dictionary<string, object>
            {
                ["spvName"] = "TestSPV",
                ["capitalId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SpvCreated", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("spvId"));
    }

    [Fact]
    public async Task ProducesDomainEvent_WithCorrectPayload()
    {
        var capitalId = Guid.NewGuid().ToString();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateSpv",
            "partition-1", new Dictionary<string, object>
            {
                ["spvName"] = "InvestSPV",
                ["capitalId"] = capitalId
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("SpvCreated", evt.EventType);
        Assert.Equal("InvestSPV", evt.Payload["spvName"]);
        Assert.Equal(capitalId, evt.Payload["capitalId"]);
        Assert.Equal("whyce.spv.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateSpv",
            "partition-1", new Dictionary<string, object>
            {
                ["spvName"] = "DeterSPV",
                ["capitalId"] = Guid.NewGuid().ToString()
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task MissingSpvName_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateSpv",
            "partition-1", new Dictionary<string, object>
            {
                ["capitalId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
