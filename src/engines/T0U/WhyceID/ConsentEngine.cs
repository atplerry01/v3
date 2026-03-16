namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

public sealed class ConsentEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityConsentStore _store;

    public ConsentEngine(
        IdentityRegistry registry,
        IdentityConsentStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityConsent GrantConsent(
        Guid identityId,
        string target,
        string scope)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be empty.");

        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be empty.");

        var consent = new IdentityConsent(
            Guid.NewGuid(),
            identityId,
            target,
            scope,
            DateTime.UtcNow,
            false);

        _store.Register(consent);

        return consent;
    }

    public bool CheckConsent(Guid identityId, string target, string scope)
    {
        return _store.HasConsent(identityId, target, scope);
    }

    public void RevokeConsent(Guid consentId)
    {
        _store.Revoke(consentId);
    }

    public IReadOnlyCollection<IdentityConsent> GetConsents(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }
}
