namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Core.Identity;
using Whycespace.Contracts.Engines;

public sealed class IdentityGraphEngineTests
{
    private readonly IdentityGraphEngine _engine = new();

    [Fact]
    public async Task AnalyzeIdentityGraph_WithNoConnections_ReturnsZeroRisk()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["connectedDevices"] = new List<string>(),
                ["connectedProviders"] = new List<Guid>(),
                ["connectedOperators"] = new List<Guid>(),
                ["connectedServices"] = new List<Guid>()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityGraphAnalyzed", result.Events[0].EventType);

        var riskScore = (double)result.Output["riskScore"];
        Assert.Equal(0.0, riskScore);
        Assert.Equal(0, (int)result.Output["connectedIdentityCount"]);
        Assert.Equal(0, (int)result.Output["sharedDeviceCount"]);
        Assert.Equal(0, (int)result.Output["suspiciousConnections"]);
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_WithSharedDevices_DetectsDeviceSharing()
    {
        var devices = new List<string> { "device-1", "device-2", "device-3", "device-4", "device-5" };

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["connectedDevices"] = devices,
                ["connectedProviders"] = new List<Guid>(),
                ["connectedOperators"] = new List<Guid>(),
                ["connectedServices"] = new List<Guid>()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(5, (int)result.Output["sharedDeviceCount"]);
        Assert.True((double)result.Output["riskScore"] > 0.0);
        Assert.True(result.Output.ContainsKey("factor.deviceSharing"));
        Assert.Equal(10.0, (double)result.Output["factor.deviceSharing"]); // (5-3)*5 = 10
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_DetectsSuspiciousCluster()
    {
        // Create a suspicious cluster: many devices, many operators, many providers
        var devices = Enumerable.Range(0, 8).Select(i => $"device-{i}").ToList();
        var providers = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();
        var operators = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        var services = Enumerable.Range(0, 15).Select(_ => Guid.NewGuid()).ToList();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["connectedDevices"] = devices,
                ["connectedProviders"] = providers,
                ["connectedOperators"] = operators,
                ["connectedServices"] = services
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var riskScore = (double)result.Output["riskScore"];
        Assert.True(riskScore > 30.0);
        Assert.True((int)result.Output["suspiciousConnections"] > 0);
        Assert.True(result.Output.ContainsKey("factor.deviceSharing"));
        Assert.True(result.Output.ContainsKey("factor.providerOverlap"));
        Assert.True(result.Output.ContainsKey("factor.operatorClustering"));
        Assert.True(result.Output.ContainsKey("factor.serviceDensity"));
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_RiskScoreNormalizedBetween0And100()
    {
        // Extreme case: lots of connections
        var devices = Enumerable.Range(0, 50).Select(i => $"device-{i}").ToList();
        var providers = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var operators = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var services = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["connectedDevices"] = devices,
                ["connectedProviders"] = providers,
                ["connectedOperators"] = operators,
                ["connectedServices"] = services
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var riskScore = (double)result.Output["riskScore"];
        Assert.InRange(riskScore, 0.0, 100.0);
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_MissingIdentityId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_EmitsEventWithCorrectAggregateId()
    {
        var identityId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = identityId.ToString(),
                ["connectedDevices"] = new List<string> { "device-1" }
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(identityId, result.Events[0].AggregateId);
        Assert.Equal("IdentityGraphAnalyzed", result.Events[0].EventType);
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_Deterministic_SameInputsSameResult()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["connectedDevices"] = new List<string> { "device-1", "device-2", "device-3", "device-4" },
            ["connectedProviders"] = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
            ["connectedOperators"] = new List<Guid>(),
            ["connectedServices"] = new List<Guid>()
        };

        var context1 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", data);

        var context2 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
            "partition-1", data);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.Equal(
            (double)result1.Output["riskScore"],
            (double)result2.Output["riskScore"]);
        Assert.Equal(
            (int)result1.Output["suspiciousConnections"],
            (int)result2.Output["suspiciousConnections"]);
    }

    [Fact]
    public async Task AnalyzeIdentityGraph_ConcurrentAnalyses_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ =>
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "AnalyzeIdentityGraph",
                "partition-1", new Dictionary<string, object>
                {
                    ["identityId"] = Guid.NewGuid().ToString(),
                    ["connectedDevices"] = new List<string> { "device-1", "device-2", "device-3", "device-4", "device-5" },
                    ["connectedProviders"] = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    ["connectedOperators"] = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToList(),
                    ["connectedServices"] = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList()
                });

            return _engine.ExecuteAsync(context);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Single(r.Events);
            var score = (double)r.Output["riskScore"];
            Assert.InRange(score, 0.0, 100.0);
        });
    }
}
