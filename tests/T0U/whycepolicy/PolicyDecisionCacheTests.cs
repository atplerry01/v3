using Whycespace.Systems.Upstream.WhycePolicy.Cache;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyDecisionCacheTests
{
    private readonly PolicyDecisionCacheStore _store = new();

    private static PolicyDecision CreateDecision(string policyId = "pol-1", bool allowed = true)
    {
        return new PolicyDecision(policyId, allowed, allowed ? "allow" : "deny", "Test reason", DateTime.UtcNow);
    }

    private static PolicyDecisionCacheEntry CreateEntry(
        string cacheKey,
        PolicyDecision? decision = null,
        TimeSpan? ttl = null)
    {
        var d = decision ?? CreateDecision();
        var now = DateTime.UtcNow;
        var expiry = now.Add(ttl ?? TimeSpan.FromMinutes(5));
        return new PolicyDecisionCacheEntry(cacheKey, new List<PolicyDecision> { d }, now, expiry);
    }

    private static PolicyContext CreateContext(
        string domain = "identity",
        Dictionary<string, string>? attributes = null)
    {
        return new PolicyContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            domain,
            attributes ?? new Dictionary<string, string> { ["trust"] = "80" },
            DateTime.UtcNow);
    }

    private static PolicyDefinition CreatePolicyDefinition(string policyId = "pol-1", int version = 1)
    {
        return new PolicyDefinition(
            policyId,
            $"Policy {policyId}",
            version,
            "identity",
            new List<PolicyCondition> { new("trust", "gte", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) },
            DateTime.UtcNow);
    }

    [Fact]
    public void CacheEntry_Creation()
    {
        var decision = CreateDecision();
        var entry = CreateEntry("test-key", decision);
        _store.Set("test-key", entry);

        var retrieved = _store.Get("test-key");

        Assert.NotNull(retrieved);
        Assert.Equal("test-key", retrieved.CacheKey);
        Assert.Equal("pol-1", retrieved.Decisions[0].PolicyId);
        Assert.True(retrieved.ExpiresAt > retrieved.CachedAt);
    }

    [Fact]
    public void CacheHit_ReturnsStoredEntry()
    {
        var decision = CreateDecision();
        var entry = CreateEntry("hit-key", decision);
        _store.Set("hit-key", entry);

        var retrieved = _store.Get("hit-key");

        Assert.NotNull(retrieved);
        Assert.Equal(decision.PolicyId, retrieved.Decisions[0].PolicyId);
        Assert.Equal(decision.Allowed, retrieved.Decisions[0].Allowed);
    }

    [Fact]
    public void CacheMiss_ReturnsNull()
    {
        var result = _store.Get("nonexistent-key");

        Assert.Null(result);
    }

    [Fact]
    public void Expiration_ReturnsNullForExpiredEntry()
    {
        var decision = CreateDecision();
        var entry = CreateEntry("expired-key", decision, TimeSpan.FromMilliseconds(1));
        _store.Set("expired-key", entry);

        Thread.Sleep(10);

        // ClearExpired must be called to remove expired entries
        _store.ClearExpired();
        var result = _store.Get("expired-key");
        Assert.Null(result);
    }

    [Fact]
    public void CacheKeyBuilder_Deterministic()
    {
        var context = CreateContext();
        var policies = new[] { CreatePolicyDefinition() };

        var key1 = PolicyDecisionCacheKeyBuilder.BuildKey(context, policies);
        var key2 = PolicyDecisionCacheKeyBuilder.BuildKey(context, policies);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void CacheKeyBuilder_DifferentPolicyVersions_DifferentKeys()
    {
        var context = CreateContext();
        var policiesV1 = new[] { CreatePolicyDefinition("pol-1", 1) };
        var policiesV2 = new[] { CreatePolicyDefinition("pol-1", 2) };

        var key1 = PolicyDecisionCacheKeyBuilder.BuildKey(context, policiesV1);
        var key2 = PolicyDecisionCacheKeyBuilder.BuildKey(context, policiesV2);

        Assert.NotEqual(key1, key2);
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
                var key = $"concurrent-key-{index}";
                var entry = CreateEntry(key, CreateDecision($"pol-{index}"));
                _store.Set(key, entry);
                var result = _store.Get(key);
                Assert.NotNull(result);
                Assert.Equal($"pol-{index}", result.Decisions[0].PolicyId);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        var all = _store.GetAll();
        Assert.Equal(100, all.Count);
    }

    [Fact]
    public void CacheInvalidation_RemovesEntry()
    {
        var decision = CreateDecision();
        var entry = CreateEntry("remove-key", decision);
        _store.Set("remove-key", entry);

        Assert.NotNull(_store.Get("remove-key"));

        _store.Invalidate("remove-key");

        Assert.Null(_store.Get("remove-key"));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _store.Set("key-1", CreateEntry("key-1", CreateDecision("pol-1")));
        _store.Set("key-2", CreateEntry("key-2", CreateDecision("pol-2")));

        Assert.Equal(2, _store.GetAll().Count);

        _store.Clear();

        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public void GetAll_ExcludesExpiredEntries_AfterCleanup()
    {
        _store.Set("fresh-key", CreateEntry("fresh-key", CreateDecision("pol-fresh")));
        _store.Set("expired-key", CreateEntry("expired-key", CreateDecision("pol-expired"), TimeSpan.FromMilliseconds(1)));

        Thread.Sleep(10);

        _store.ClearExpired();
        var all = _store.GetAll();
        Assert.Single(all);
        Assert.Equal("pol-fresh", all[0].Decisions[0].PolicyId);
    }
}
