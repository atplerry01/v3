namespace Whycespace.Systems.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Models;

public sealed class IdentityRevocationStore
{
    private readonly ConcurrentDictionary<Guid, IdentityRevocation> _revocations = new();

    public void Register(IdentityRevocation revocation)
    {
        _revocations[revocation.RevocationId] = revocation;
    }

    public bool IsRevoked(Guid identityId)
    {
        return _revocations.Values.Any(r =>
            r.IdentityId == identityId &&
            r.Active);
    }

    public IReadOnlyCollection<IdentityRevocation> GetByIdentity(Guid identityId)
    {
        return _revocations.Values
            .Where(r => r.IdentityId == identityId)
            .ToList();
    }

    public IReadOnlyCollection<IdentityRevocation> GetAll()
    {
        return _revocations.Values.ToList();
    }
}
