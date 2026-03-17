namespace Whycespace.AccessEngines.Tests;

using Whycespace.Engines.T4A.Tools.Developer;
using Whycespace.Contracts.Engines;

public sealed class DeveloperToolsEngineTests
{
    private readonly DeveloperToolsEngine _engine = new();

    [Fact]
    public async Task InspectsWorkflow_Successfully()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DevTool",
            "partition-1", new Dictionary<string, object>
            {
                ["tool"] = "workflow.inspect",
                ["targetWorkflowId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("DevWorkflowInspected", result.Events[0].EventType);
        Assert.Equal(true, result.Output["dispatched"]);
    }

    [Fact]
    public async Task BlocksTools_InProductionEnvironment()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DevTool",
            "partition-1", new Dictionary<string, object>
            {
                ["tool"] = "workflow.inspect",
                ["environment"] = "production",
                ["targetWorkflowId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DumpsContext_WithAllKeys()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DevTool",
            "partition-1", new Dictionary<string, object>
            {
                ["tool"] = "context.dump",
                ["extra"] = "value"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("dataKeyCount"));
    }

    [Fact]
    public async Task Fails_WhenUnknownTool()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DevTool",
            "partition-1", new Dictionary<string, object>
            {
                ["tool"] = "unknown.tool"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
