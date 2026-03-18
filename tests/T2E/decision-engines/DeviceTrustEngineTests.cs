namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Atlas.Identity.Engines;
using Whycespace.Engines.T3I.Atlas.Identity.Models;
using Whycespace.Contracts.Engines;

public sealed class DeviceTrustEngineTests
{
    private readonly DeviceTrustEngine _engine = new();

    [Fact]
    public async Task EvaluateDeviceTrust_TrustedDevice_ReturnsHighScore()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-001",
                ["deviceFingerprint"] = "fp-abc123",
                ["deviceType"] = "Desktop",
                ["operatingSystem"] = "Windows 11",
                ["ipAddress"] = "192.168.1.1",
                ["geoLocation"] = "London, UK",
                ["previousDeviceUsageCount"] = 50,
                ["deviceAgeDays"] = 365
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("DeviceTrustEvaluated", result.Events[0].EventType);

        var trustScore = (double)result.Output["trustScore"];
        Assert.Equal("High", result.Output["trustLevel"]);
        Assert.InRange(trustScore, 70.0, 100.0);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_UnknownDevice_ReturnsLowScore()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-unknown",
                ["deviceFingerprint"] = "",
                ["deviceType"] = "Unknown",
                ["operatingSystem"] = "",
                ["ipAddress"] = "",
                ["geoLocation"] = "",
                ["previousDeviceUsageCount"] = 0,
                ["deviceAgeDays"] = 0
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var trustScore = (double)result.Output["trustScore"];
        Assert.Equal("Low", result.Output["trustLevel"]);
        Assert.InRange(trustScore, 0.0, 39.99);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_SuspiciousIndicators_DetectsRisks()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-suspicious",
                ["deviceFingerprint"] = "",
                ["deviceType"] = "Mobile",
                ["operatingSystem"] = "Android",
                ["ipAddress"] = "",
                ["geoLocation"] = "",
                ["previousDeviceUsageCount"] = 0,
                ["deviceAgeDays"] = 0
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var riskIndicators = (List<string>)result.Output["riskIndicators"];
        Assert.NotEmpty(riskIndicators);
        Assert.Contains(riskIndicators, r => r.Contains("Unknown device"));
        Assert.Contains(riskIndicators, r => r.Contains("fingerprint"));
        Assert.Contains(riskIndicators, r => r.Contains("IP address"));
        Assert.Contains(riskIndicators, r => r.Contains("geolocation"));
    }

    [Fact]
    public async Task EvaluateDeviceTrust_ScoreNormalization_StaysWithinBounds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-max",
                ["deviceFingerprint"] = "fp-stable",
                ["deviceType"] = "Desktop",
                ["operatingSystem"] = "macOS",
                ["ipAddress"] = "10.0.0.1",
                ["geoLocation"] = "New York, US",
                ["previousDeviceUsageCount"] = 9999,
                ["deviceAgeDays"] = 9999
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var trustScore = (double)result.Output["trustScore"];
        Assert.InRange(trustScore, 0.0, 100.0);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_TrustLevelClassification_Correct()
    {
        // Test Low (no data)
        var lowContext = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-low",
                ["previousDeviceUsageCount"] = 0,
                ["deviceAgeDays"] = 0
            });

        var lowResult = await _engine.ExecuteAsync(lowContext);
        Assert.Equal("Low", lowResult.Output["trustLevel"]);

        // Test Medium (partial data)
        var medContext = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-med",
                ["deviceFingerprint"] = "fp-med",
                ["ipAddress"] = "10.0.0.1",
                ["geoLocation"] = "London, UK",
                ["previousDeviceUsageCount"] = 5,
                ["deviceAgeDays"] = 30
            });

        var medResult = await _engine.ExecuteAsync(medContext);
        Assert.Equal("Medium", medResult.Output["trustLevel"]);

        // Test High (full data)
        var highContext = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["deviceId"] = "device-high",
                ["deviceFingerprint"] = "fp-high",
                ["ipAddress"] = "10.0.0.1",
                ["geoLocation"] = "London, UK",
                ["previousDeviceUsageCount"] = 50,
                ["deviceAgeDays"] = 365
            });

        var highResult = await _engine.ExecuteAsync(highContext);
        Assert.Equal("High", highResult.Output["trustLevel"]);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_MissingIdentityId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["deviceId"] = "device-001"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_MissingDeviceId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_EmitsEventWithCorrectAggregateId()
    {
        var identityId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = identityId.ToString(),
                ["deviceId"] = "device-evt",
                ["deviceFingerprint"] = "fp-test",
                ["previousDeviceUsageCount"] = 10,
                ["deviceAgeDays"] = 30
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(identityId, result.Events[0].AggregateId);
        Assert.Equal("DeviceTrustEvaluated", result.Events[0].EventType);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_Deterministic_SameInputsSameScore()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["deviceId"] = "device-det",
            ["deviceFingerprint"] = "fp-det",
            ["deviceType"] = "Tablet",
            ["operatingSystem"] = "iPadOS",
            ["ipAddress"] = "172.16.0.1",
            ["geoLocation"] = "Berlin, DE",
            ["previousDeviceUsageCount"] = 25,
            ["deviceAgeDays"] = 180
        };

        var context1 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", data);

        var context2 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
            "partition-1", data);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.Equal(
            (double)result1.Output["trustScore"],
            (double)result2.Output["trustScore"]);
    }

    [Fact]
    public async Task EvaluateDeviceTrust_ConcurrentEvaluations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ =>
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateDeviceTrust",
                "partition-1", new Dictionary<string, object>
                {
                    ["identityId"] = Guid.NewGuid().ToString(),
                    ["deviceId"] = $"device-{Guid.NewGuid():N}",
                    ["deviceFingerprint"] = "fp-concurrent",
                    ["deviceType"] = "Mobile",
                    ["operatingSystem"] = "iOS",
                    ["ipAddress"] = "10.0.0.1",
                    ["geoLocation"] = "Tokyo, JP",
                    ["previousDeviceUsageCount"] = 15,
                    ["deviceAgeDays"] = 90
                });

            return _engine.ExecuteAsync(context);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Single(r.Events);
            var score = (double)r.Output["trustScore"];
            Assert.InRange(score, 0.0, 100.0);
        });
    }
}
