namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDecisionCacheEntry(
    string CacheKey,
    IReadOnlyList<PolicyDecision> Decisions,
    DateTime CachedAt,
    DateTime ExpiresAt
);
