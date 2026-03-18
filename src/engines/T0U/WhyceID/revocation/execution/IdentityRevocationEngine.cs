namespace Whycespace.Engines.T0U.WhyceID.Revocation.Execution;

using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

public sealed class IdentityRevocationEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRevocationStore _store;

    public IdentityRevocationEngine(
        IdentityRegistry registry,
        IdentityRevocationStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityRevocation RevokeIdentity(Guid identityId, string reason)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Revocation reason cannot be empty.");

        var revocation = new IdentityRevocation(
            Guid.NewGuid(),
            identityId,
            reason,
            DateTime.UtcNow,
            true
        );

        _store.Register(revocation);

        return revocation;
    }

    public bool IsIdentityRevoked(Guid identityId)
    {
        return _store.IsRevoked(identityId);
    }

    public IReadOnlyCollection<IdentityRevocation> GetRevocations(Guid identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyCollection<IdentityRevocation> GetAllRevocations()
    {
        return _store.GetAll();
    }
}
