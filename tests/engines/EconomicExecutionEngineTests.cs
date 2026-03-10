namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E_Execution;
using Whycespace.Shared.Contracts;
using Xunit;

public sealed class EconomicExecutionEngineTests
{
    private readonly EconomicExecutionEngine _engine = new();

    [Fact]
    public async Task AllocateCapital_WithAmount_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AllocateCapital",
            "partition-1", new Dictionary<string, object> { ["amount"] = 10000m });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal("CapitalAllocated", result.Events[0].EventType);
    }

    [Fact]
    public async Task AllocateCapital_WithoutAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AllocateCapital",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSpv_WithName_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateSpv",
            "partition-1", new Dictionary<string, object> { ["spvName"] = "TestSPV" });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal("SpvCreated", result.Events[0].EventType);
    }
}
