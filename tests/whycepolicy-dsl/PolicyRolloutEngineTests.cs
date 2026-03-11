using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyRolloutEngineTests
{
    private readonly PolicyRolloutStore _store = new();
    private readonly PolicyRolloutEngine _engine;

    public PolicyRolloutEngineTests()
    {
        _engine = new PolicyRolloutEngine(_store);
    }

    [Fact]
    public void GlobalRollout_AppliesToAllActors()
    {
        _store.SetRolloutConfig(new PolicyRolloutConfig(
            "pol-1", "1", PolicyRolloutStrategy.Global, 0,
            Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow));

        Assert.True(_engine.IsPolicyActiveForActor("pol-1", "1", "actor-1", "identity"));
        Assert.True(_engine.IsPolicyActiveForActor("pol-1", "1", "actor-2", "economic"));
        Assert.True(_engine.IsPolicyActiveForActor("pol-1", "1", "actor-999", "any-domain"));
    }

    [Fact]
    public void PercentageRollout_Deterministic()
    {
        _store.SetRolloutConfig(new PolicyRolloutConfig(
            "pol-2", "1", PolicyRolloutStrategy.Percentage, 50,
            Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow));

        var result1 = _engine.IsPolicyActiveForActor("pol-2", "1", "actor-a", "identity");
        var result2 = _engine.IsPolicyActiveForActor("pol-2", "1", "actor-a", "identity");

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void ActorListRollout_OnlyListedActors()
    {
        _store.SetRolloutConfig(new PolicyRolloutConfig(
            "pol-3", "1", PolicyRolloutStrategy.ActorList, 0,
            new List<string> { "actor-1", "actor-2" }, Array.Empty<string>(), DateTime.UtcNow));

        Assert.True(_engine.IsPolicyActiveForActor("pol-3", "1", "actor-1", "identity"));
        Assert.True(_engine.IsPolicyActiveForActor("pol-3", "1", "actor-2", "identity"));
        Assert.False(_engine.IsPolicyActiveForActor("pol-3", "1", "actor-3", "identity"));
    }

    [Fact]
    public void DomainListRollout_OnlyListedDomains()
    {
        _store.SetRolloutConfig(new PolicyRolloutConfig(
            "pol-4", "1", PolicyRolloutStrategy.DomainList, 0,
            Array.Empty<string>(), new List<string> { "identity", "economic" }, DateTime.UtcNow));

        Assert.True(_engine.IsPolicyActiveForActor("pol-4", "1", "actor-1", "identity"));
        Assert.True(_engine.IsPolicyActiveForActor("pol-4", "1", "actor-1", "economic"));
        Assert.False(_engine.IsPolicyActiveForActor("pol-4", "1", "actor-1", "cluster"));
    }

    [Fact]
    public void PercentageRollout_ConsistentAcrossActors()
    {
        _store.SetRolloutConfig(new PolicyRolloutConfig(
            "pol-5", "1", PolicyRolloutStrategy.Percentage, 50,
            Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow));

        var results = new Dictionary<string, bool>();
        for (var i = 0; i < 100; i++)
        {
            var actorId = $"actor-{i}";
            results[actorId] = _engine.IsPolicyActiveForActor("pol-5", "1", actorId, "identity");
        }

        // Verify determinism: same actor always gets same result
        for (var i = 0; i < 100; i++)
        {
            var actorId = $"actor-{i}";
            Assert.Equal(results[actorId], _engine.IsPolicyActiveForActor("pol-5", "1", actorId, "identity"));
        }

        // Verify rough distribution (not all true or all false)
        var trueCount = results.Values.Count(v => v);
        Assert.True(trueCount > 10 && trueCount < 90);
    }

    [Fact]
    public void RolloutConfig_StoredCorrectly()
    {
        var config = new PolicyRolloutConfig(
            "pol-6", "1", PolicyRolloutStrategy.ActorList, 0,
            new List<string> { "a", "b" }, Array.Empty<string>(), DateTime.UtcNow);

        _store.SetRolloutConfig(config);

        var retrieved = _store.GetRolloutConfig("pol-6", "1");
        Assert.NotNull(retrieved);
        Assert.Equal(PolicyRolloutStrategy.ActorList, retrieved.Strategy);
        Assert.Equal(2, retrieved.Actors.Count);
    }

    [Fact]
    public void MissingRolloutConfig_DefaultsToGlobal()
    {
        Assert.True(_engine.IsPolicyActiveForActor("no-config", "1", "any-actor", "any-domain"));
    }
}
