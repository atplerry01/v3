namespace Whycespace.Engines.T0U.WhyceGovernance;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Registry;

public sealed class GuardianRegistryEngine
{
    private readonly GuardianRegistryStore _store;
    private readonly IdentityRegistry _identityRegistry;

    public GuardianRegistryEngine(
        GuardianRegistryStore store,
        IdentityRegistry identityRegistry)
    {
        _store = store;
        _identityRegistry = identityRegistry;
    }

    public Guardian RegisterGuardian(string guardianId, Guid identityId, string name, IReadOnlyList<string> roles)
    {
        if (!_identityRegistry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        var guardian = new Guardian(
            guardianId,
            identityId,
            name,
            GuardianStatus.Registered,
            roles,
            DateTime.UtcNow,
            ActivatedAt: null);

        _store.Register(guardian);
        return guardian;
    }

    public Guardian ActivateGuardian(string guardianId)
    {
        _store.ActivateGuardian(guardianId);
        return _store.GetGuardian(guardianId)!;
    }

    public Guardian DeactivateGuardian(string guardianId)
    {
        _store.DeactivateGuardian(guardianId);
        return _store.GetGuardian(guardianId)!;
    }

    public Guardian GetGuardian(string guardianId)
    {
        var guardian = _store.GetGuardian(guardianId);
        if (guardian is null)
            throw new KeyNotFoundException($"Guardian not found: {guardianId}");
        return guardian;
    }

    public IReadOnlyList<Guardian> ListGuardians()
    {
        return _store.ListGuardians();
    }
}
