namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class TrustScoreEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityTrustStore _store;

    public TrustScoreEngine(
        IdentityRegistry registry,
        IdentityTrustStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityTrustScore Calculate(Guid identityId)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}");
        }

        var identity = _registry.Get(identityId);

        int score = 0;

        if (identity.Status == IdentityStatus.Verified)
        {
            score += 50;
        }

        var age = DateTime.UtcNow - identity.CreatedAt;

        if (age.TotalDays > 30)
        {
            score += 25;
        }

        if (age.TotalDays > 180)
        {
            score += 25;
        }

        var trust = new IdentityTrustScore(
            score,
            DateTime.UtcNow);

        _store.Update(identityId, trust);

        return trust;
    }

    public IdentityTrustScore? Get(Guid identityId)
    {
        return _store.Get(identityId);
    }
}
