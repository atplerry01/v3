namespace Whycespace.Engines.T0U.WhycePolicy;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyDecisionCacheEngine
{
    private readonly PolicyDecisionCacheStore _store;
    private readonly TimeSpan _ttl;

    public PolicyDecisionCacheEngine(PolicyDecisionCacheStore store)
        : this(store, TimeSpan.FromSeconds(60))
    {
    }

    public PolicyDecisionCacheEngine(PolicyDecisionCacheStore store, TimeSpan ttl)
    {
        _store = store;
        _ttl = ttl;
    }

    public string GenerateCacheKey(string domain, string actorId, IReadOnlyDictionary<string, string> attributes)
    {
        var hash = ComputeAttributeHash(attributes);
        return $"{domain}:{actorId}:{hash}";
    }

    public IReadOnlyList<PolicyDecision>? GetCachedDecision(string cacheKey)
    {
        var entry = _store.Get(cacheKey);
        if (entry is null)
            return null;

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _store.Invalidate(cacheKey);
            return null;
        }

        return entry.Decisions;
    }

    public void StoreDecision(string cacheKey, IReadOnlyList<PolicyDecision> decisions)
    {
        var now = DateTime.UtcNow;
        var entry = new PolicyDecisionCacheEntry(cacheKey, decisions, now, now.Add(_ttl));
        _store.Set(cacheKey, entry);
    }

    public void Invalidate(string cacheKey)
    {
        _store.Invalidate(cacheKey);
    }

    private static string ComputeAttributeHash(IReadOnlyDictionary<string, string> attributes)
    {
        var sorted = attributes.OrderBy(kvp => kvp.Key, StringComparer.Ordinal);
        var sb = new StringBuilder();
        foreach (var kvp in sorted)
        {
            sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..8];
    }
}
