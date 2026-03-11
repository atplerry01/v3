namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Stores;

public sealed class FederationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityFederationStore _store;

    public FederationEngine(
        IdentityRegistry registry,
        IdentityFederationStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityFederation RegisterFederation(
        string provider,
        string externalIdentityId,
        Guid internalIdentityId)
    {
        if (!_registry.Exists(internalIdentityId))
            throw new InvalidOperationException($"Identity does not exist: {internalIdentityId}");

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty.");

        if (string.IsNullOrWhiteSpace(externalIdentityId))
            throw new ArgumentException("External identity id cannot be empty.");

        var federation = new IdentityFederation(
            Guid.NewGuid(),
            provider,
            externalIdentityId,
            internalIdentityId,
            DateTime.UtcNow,
            false
        );

        _store.Register(federation);

        return federation;
    }

    public bool ValidateFederation(string provider, string externalIdentityId)
    {
        return _store.Validate(provider, externalIdentityId);
    }

    public IdentityFederation? GetFederation(string provider, string externalIdentityId)
    {
        return _store.Get(provider, externalIdentityId);
    }

    public void RevokeFederation(Guid federationId)
    {
        _store.Revoke(federationId);
    }

    public IReadOnlyCollection<IdentityFederation> GetFederations()
    {
        return _store.GetAll();
    }
}
