namespace Whycespace.Systems.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Models;

public sealed class IdentityTrustStore
{
    private readonly ConcurrentDictionary<Guid, IdentityTrustScore> _scores = new();

    public void Update(Guid identityId, IdentityTrustScore score)
    {
        _scores[identityId] = score;
    }

    public IdentityTrustScore? Get(Guid identityId)
    {
        if (_scores.TryGetValue(identityId, out var score))
        {
            return score;
        }

        return null;
    }
}
