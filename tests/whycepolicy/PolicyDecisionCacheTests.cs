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
        _store.Set("test-key", decision, TimeSpan.FromMinutes(5));

        var entry = _store.Get("test-key");

        Assert.NotNull(entry);
        Assert.Equal("test-key", entry.CacheKey);
        Assert.Equal("pol-1", entry.Decision.PolicyId);
        Assert.True(entry.ExpiresAt > entry.CreatedAt);
    }

    [Fact]
    public void CacheHit_ReturnsStoredEntry()
    {
        var decision = CreateDecision();
        _store.Set("hit-key", decision, TimeSpan.FromMinutes(5));

        var entry = _store.Get("hit-key");

        Assert.NotNull(entry);
        Assert.Equal(decision.PolicyId, entry.Decision.PolicyId);
        Assert.Equal(decision.Allowed, entry.Decision.Allowed);
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
        _store.Set("expired-key", decision, TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10);

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
                _store.Set(key, CreateDecision($"pol-{index}"), TimeSpan.FromMinutes(5));
                var result = _store.Get(key);
                Assert.NotNull(result);
                Assert.Equal($"pol-{index}", result.Decision.PolicyId);
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
        _store.Set("remove-key", decision, TimeSpan.FromMinutes(5));

        Assert.NotNull(_store.Get("remove-key"));

        _store.Remove("remove-key");

        Assert.Null(_store.Get("remove-key"));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _store.Set("key-1", CreateDecision("pol-1"), TimeSpan.FromMinutes(5));
        _store.Set("key-2", CreateDecision("pol-2"), TimeSpan.FromMinutes(5));

        Assert.Equal(2, _store.GetAll().Count);

        _store.Clear();

        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public void GetAll_ExcludesExpiredEntries()
    {
        _store.Set("fresh-key", CreateDecision("pol-fresh"), TimeSpan.FromMinutes(5));
        _store.Set("expired-key", CreateDecision("pol-expired"), TimeSpan.FromMilliseconds(1));

        Thread.Sleep(10);

        var all = _store.GetAll();
        Assert.Single(all);
        Assert.Equal("pol-fresh", all[0].Decision.PolicyId);
    }
}
