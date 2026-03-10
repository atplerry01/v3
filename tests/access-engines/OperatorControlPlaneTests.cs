namespace Whycespace.AccessEngines.Tests;

using Whycespace.Engines.T4A_Access;
using Whycespace.Contracts.Engines;

public sealed class OperatorControlPlaneTests
{
    private readonly OperatorControlPlaneEngine _engine = new();

    [Fact]
    public async Task ListsClusters_WithOperatorRole()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "OperatorAction",
            "partition-1", new Dictionary<string, object>
            {
                ["operation"] = "cluster.list",
                ["operatorId"] = "op-1",
                ["operatorRole"] = "Operator"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("OperatorClusterCommand", result.Events[0].EventType);
        Assert.Equal(true, result.Output["dispatched"]);
    }

    [Fact]
    public async Task DeniesAccess_WhenNonAdminAttemptsRestrictedOperation()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "OperatorAction",
            "partition-1", new Dictionary<string, object>
            {
                ["operation"] = "cluster.suspend",
                ["operatorId"] = "op-1",
                ["operatorRole"] = "Operator"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
    }

    [Fact]
    public async Task AllowsRestrictedOperation_ForSystemAdmin()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "OperatorAction",
            "partition-1", new Dictionary<string, object>
            {
                ["operation"] = "cluster.suspend",
                ["operatorId"] = "admin-1",
                ["operatorRole"] = "SystemAdmin",
                ["clusterId"] = "cluster-abc"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["dispatched"]);
    }

    [Fact]
    public async Task Fails_WhenMissingOperatorId()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "OperatorAction",
            "partition-1", new Dictionary<string, object>
            {
                ["operation"] = "engine.list"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
