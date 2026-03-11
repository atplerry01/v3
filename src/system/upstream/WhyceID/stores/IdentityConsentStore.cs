namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityConsentStore
{
    private readonly ConcurrentDictionary<Guid, IdentityConsent> _consents = new();

    public void Register(IdentityConsent consent)
    {
        _consents[consent.ConsentId] = consent;
    }

    public IReadOnlyCollection<IdentityConsent> GetByIdentity(Guid identityId)
    {
        return _consents.Values
            .Where(c => c.IdentityId == identityId)
            .ToList();
    }

    public bool HasConsent(Guid identityId, string target, string scope)
    {
        return _consents.Values.Any(c =>
            c.IdentityId == identityId &&
            c.Target == target &&
            c.Scope == scope &&
            !c.Revoked);
    }

    public void Revoke(Guid consentId)
    {
        if (_consents.TryGetValue(consentId, out var consent))
        {
            _consents[consentId] = consent with { Revoked = true };
        }
    }
}
