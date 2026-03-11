namespace Whycespace.System.Upstream.WhycePolicy.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed class PolicyDecisionCacheStore
{
    private readonly ConcurrentDictionary<string, PolicyDecisionCacheEntry> _store = new();

    public void Set(string cacheKey, PolicyDecisionCacheEntry entry)
    {
        _store[cacheKey] = entry;
    }

    public PolicyDecisionCacheEntry? Get(string cacheKey)
    {
        return _store.TryGetValue(cacheKey, out var entry) ? entry : null;
    }

    public void Invalidate(string cacheKey)
    {
        _store.TryRemove(cacheKey, out _);
    }

    public void ClearExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _store.Keys)
        {
            if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt <= now)
            {
                _store.TryRemove(key, out _);
            }
        }
    }

    public IReadOnlyList<PolicyDecisionCacheEntry> GetAll()
    {
        return _store.Values.ToList();
    }

    public void Clear()
    {
        _store.Clear();
    }
}
