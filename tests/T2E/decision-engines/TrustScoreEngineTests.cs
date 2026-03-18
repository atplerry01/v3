namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Atlas.Identity;
using Whycespace.Contracts.Engines;

public sealed class TrustScoreEngineTests
{
    private readonly TrustScoreEngine _engine = new();

    [Fact]
    public async Task EvaluateTrustScore_WithFullVerification_ReturnsHighScore()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["verifiedEmail"] = true,
                ["verifiedPhone"] = true,
                ["verifiedDocuments"] = true,
                ["deviceTrustScore"] = 1.0,
                ["accountAgeDays"] = 365,
                ["behaviorScore"] = 1.0
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("TrustScoreEvaluated", result.Events[0].EventType);

        var trustScore = (double)result.Output["trustScore"];
        Assert.Equal(85.0, trustScore);
    }

    [Fact]
    public async Task EvaluateTrustScore_WithMinimalVerification_ReturnsLowScore()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["verifiedEmail"] = false,
                ["verifiedPhone"] = false,
                ["verifiedDocuments"] = false,
                ["deviceTrustScore"] = 0.0,
                ["accountAgeDays"] = 0,
                ["behaviorScore"] = 0.0
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var trustScore = (double)result.Output["trustScore"];
        Assert.Equal(0.0, trustScore);
    }

    [Fact]
    public async Task EvaluateTrustScore_ScoreNormalizedBetween0And100()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["verifiedEmail"] = true,
                ["verifiedPhone"] = true,
                ["verifiedDocuments"] = true,
                ["deviceTrustScore"] = 1.0,
                ["accountAgeDays"] = 9999,
                ["behaviorScore"] = 1.0
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var trustScore = (double)result.Output["trustScore"];
        Assert.InRange(trustScore, 0.0, 100.0);
    }

    [Fact]
    public async Task EvaluateTrustScore_ScoreFactorsWeightedCorrectly()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = Guid.NewGuid().ToString(),
                ["verifiedEmail"] = true,
                ["verifiedPhone"] = false,
                ["verifiedDocuments"] = true,
                ["deviceTrustScore"] = 0.5,
                ["accountAgeDays"] = 180,
                ["behaviorScore"] = 0.75
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);

        // Email: 10, Phone: 0, Documents: 20, Device: 7.5, Age: ~4.93, Behavior: 15
        Assert.Equal(10.0, (double)result.Output["factor.emailVerification"]);
        Assert.Equal(20.0, (double)result.Output["factor.documentVerification"]);
        Assert.Equal(7.5, (double)result.Output["factor.deviceTrust"]);
        Assert.Equal(15.0, (double)result.Output["factor.behaviorScore"]);

        Assert.False(result.Output.ContainsKey("factor.phoneVerification"));
    }

    [Fact]
    public async Task EvaluateTrustScore_MissingIdentityId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task EvaluateTrustScore_EmitsEventWithCorrectAggregateId()
    {
        var identityId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", new Dictionary<string, object>
            {
                ["identityId"] = identityId.ToString(),
                ["verifiedEmail"] = true
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(identityId, result.Events[0].AggregateId);
        Assert.Equal("TrustScoreEvaluated", result.Events[0].EventType);
    }

    [Fact]
    public async Task EvaluateTrustScore_Deterministic_SameInputsSameScore()
    {
        var data = new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["verifiedEmail"] = true,
            ["verifiedPhone"] = true,
            ["verifiedDocuments"] = false,
            ["deviceTrustScore"] = 0.8,
            ["accountAgeDays"] = 90,
            ["behaviorScore"] = 0.6
        };

        var context1 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", data);

        var context2 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
            "partition-1", data);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.Equal(
            (double)result1.Output["trustScore"],
            (double)result2.Output["trustScore"]);
    }

    [Fact]
    public async Task EvaluateTrustScore_ConcurrentEvaluations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ =>
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateTrustScore",
                "partition-1", new Dictionary<string, object>
                {
                    ["identityId"] = Guid.NewGuid().ToString(),
                    ["verifiedEmail"] = true,
                    ["verifiedPhone"] = true,
                    ["verifiedDocuments"] = true,
                    ["deviceTrustScore"] = 0.9,
                    ["accountAgeDays"] = 200,
                    ["behaviorScore"] = 0.85
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
