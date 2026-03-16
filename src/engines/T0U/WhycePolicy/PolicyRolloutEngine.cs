namespace Whycespace.Engines.T0U.WhycePolicy;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyRolloutEngine
{
    private readonly PolicyRolloutStore _store;

    public PolicyRolloutEngine(PolicyRolloutStore store)
    {
        _store = store;
    }

    public bool IsPolicyActiveForActor(string policyId, string version, string actorId, string domain)
    {
        var config = _store.GetRolloutConfig(policyId, version);

        if (config is null)
            return true; // default to global

        return config.Strategy switch
        {
            PolicyRolloutStrategy.Global => true,
            PolicyRolloutStrategy.Percentage => IsInPercentage(actorId, config.Percentage),
            PolicyRolloutStrategy.ActorList => config.Actors.Contains(actorId),
            PolicyRolloutStrategy.DomainList => config.Domains.Contains(domain),
            _ => true
        };
    }

    private static bool IsInPercentage(string actorId, int percentage)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(actorId));
        var value = Math.Abs(BitConverter.ToInt32(hash, 0)) % 100;
        return value < percentage;
    }
}
