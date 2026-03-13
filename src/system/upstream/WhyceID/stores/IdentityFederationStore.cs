namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityFederationStore
{
    private readonly ConcurrentDictionary<Guid, IdentityFederation> _federations = new();

    public void Register(IdentityFederation federation)
    {
        _federations[federation.FederationId] = federation;
    }

    public bool Validate(string provider, string externalIdentityId)
    {
        return _federations.Values.Any(f =>
            f.Provider == provider &&
            f.ExternalIdentityId == externalIdentityId &&
            !f.Revoked);
    }

    public IdentityFederation? Get(string provider, string externalIdentityId)
    {
        return _federations.Values.FirstOrDefault(f =>
            f.Provider == provider &&
            f.ExternalIdentityId == externalIdentityId &&
            !f.Revoked);
    }

    public void Revoke(Guid federationId)
    {
        if (_federations.TryGetValue(federationId, out var federation))
        {
            _federations[federationId] = federation with { Revoked = true };
        }
    }

    public IReadOnlyCollection<IdentityFederation> GetAll()
    {
        return _federations.Values.ToList();
    }
}
