namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.System.Cluster.Engines;
using Whycespace.Contracts.Engines;

public sealed class ClusterCreationEngineTests
{
    private readonly ClusterCreationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateCluster",
            "partition-1", new Dictionary<string, object>
            {
                ["clusterName"] = "TestCluster",
                ["region"] = "eu-west-1",
                ["clusterType"] = "Mobility"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ClusterCreated", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("clusterId"));
    }

    [Fact]
    public async Task ProducesDomainEvent_WithCorrectPayload()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateCluster",
            "partition-1", new Dictionary<string, object>
            {
                ["clusterName"] = "EconCluster",
                ["region"] = "us-east-1"
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("ClusterCreated", evt.EventType);
        Assert.Equal("EconCluster", evt.Payload["clusterName"]);
        Assert.Equal("us-east-1", evt.Payload["region"]);
        Assert.Equal("whyce.cluster.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var invocationId = Guid.NewGuid();
        var workflowId = Guid.NewGuid().ToString();

        var context = new EngineContext(
            invocationId, workflowId, "CreateCluster",
            "partition-1", new Dictionary<string, object>
            {
                ["clusterName"] = "DeterCluster",
                ["region"] = "ap-south-1"
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task MissingClusterName_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateCluster",
            "partition-1", new Dictionary<string, object>
            {
                ["region"] = "eu-west-1"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
