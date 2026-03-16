using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyDecisionCacheTests
{
    private readonly PolicyDecisionCacheStore _store = new();
    private readonly PolicyDecisionCacheEngine _engine;

    public PolicyDecisionCacheTests()
    {
        _engine = new PolicyDecisionCacheEngine(_store);
    }

    private static List<PolicyDecision> CreateDecisions(string policyId = "pol-1", bool allowed = true)
    {
        return new List<PolicyDecision>
        {
            new(policyId, allowed, allowed ? "allow" : "deny", "Test reason", DateTime.UtcNow)
        };
    }

    [Fact]
    public void StoreDecision_CacheEntryStored()
    {
        var key = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        var decisions = CreateDecisions();

        _engine.StoreDecision(key, decisions);

        var entry = _store.Get(key);
        Assert.NotNull(entry);
        Assert.Single(entry.Decisions);
    }

    [Fact]
    public void GetCachedDecision_ReturnsStoredDecisions()
    {
        var key = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        var decisions = CreateDecisions();
        _engine.StoreDecision(key, decisions);

        var cached = _engine.GetCachedDecision(key);

        Assert.NotNull(cached);
        Assert.Single(cached);
        Assert.Equal("pol-1", cached[0].PolicyId);
    }

    [Fact]
    public void GetCachedDecision_CacheMiss_ReturnsNull()
    {
        var result = _engine.GetCachedDecision("nonexistent:key:AABBCCDD");

        Assert.Null(result);
    }

    [Fact]
    public void Invalidate_RemovesCacheEntry()
    {
        var key = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        _engine.StoreDecision(key, CreateDecisions());

        _engine.Invalidate(key);

        Assert.Null(_engine.GetCachedDecision(key));
    }

    [Fact]
    public void GetCachedDecision_Expired_ReturnsNull()
    {
        var expiredEngine = new PolicyDecisionCacheEngine(_store, TimeSpan.FromMilliseconds(1));
        var key = expiredEngine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        expiredEngine.StoreDecision(key, CreateDecisions());

        Thread.Sleep(10);

        var result = expiredEngine.GetCachedDecision(key);
        Assert.Null(result);
    }

    [Fact]
    public void GenerateCacheKey_Deterministic()
    {
        var attrs = new Dictionary<string, string> { ["trust"] = "80", ["region"] = "us" };

        var key1 = _engine.GenerateCacheKey("identity", "123", attrs);
        var key2 = _engine.GenerateCacheKey("identity", "123", attrs);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_DifferentAttributes_DifferentKeys()
    {
        var key1 = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        var key2 = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "50" });

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void StoreDecision_MultipleEntries_AllRetrievable()
    {
        var key1 = _engine.GenerateCacheKey("identity", "123", new Dictionary<string, string> { ["trust"] = "80" });
        var key2 = _engine.GenerateCacheKey("economic", "456", new Dictionary<string, string> { ["risk"] = "low" });

        _engine.StoreDecision(key1, CreateDecisions("pol-1"));
        _engine.StoreDecision(key2, CreateDecisions("pol-2"));

        Assert.NotNull(_engine.GetCachedDecision(key1));
        Assert.NotNull(_engine.GetCachedDecision(key2));
        Assert.Equal("pol-1", _engine.GetCachedDecision(key1)![0].PolicyId);
        Assert.Equal("pol-2", _engine.GetCachedDecision(key2)![0].PolicyId);
    }

    [Fact]
    public void ConcurrentAccess_ThreadSafe()
    {
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var key = _engine.GenerateCacheKey("identity", index.ToString(), new Dictionary<string, string> { ["v"] = index.ToString() });
                _engine.StoreDecision(key, CreateDecisions($"pol-{index}"));
                var result = _engine.GetCachedDecision(key);
                Assert.NotNull(result);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        var all = _store.GetAll();
        Assert.Equal(100, all.Count);
    }
}
