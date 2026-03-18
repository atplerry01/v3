namespace Whycespace.Systems.Upstream.Governance.Registry;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GuardianRegistry : IGuardianRegistry
{
    private readonly IGuardianRegistryStore _store;

    public GuardianRegistry(IGuardianRegistryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public void RegisterGuardian(GuardianRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.AuthorityDomains.Count == 0)
            throw new InvalidOperationException("Guardian must have at least one authority domain.");

        if (!Enum.IsDefined(record.GuardianRole))
            throw new InvalidOperationException($"Invalid guardian role: {record.GuardianRole}");

        if (_store.ExistsById(record.GuardianId))
            throw new InvalidOperationException($"Guardian already registered: {record.GuardianId}");

        if (_store.ExistsByIdentity(record.IdentityId))
            throw new InvalidOperationException($"Identity already has a registered guardian: {record.IdentityId}");

        _store.Save(record);
    }

    public GuardianRecord? GetGuardian(Guid guardianId)
    {
        return _store.GetById(guardianId);
    }

    public GuardianRecord? GetGuardianByIdentity(string identityId)
    {
        return _store.GetByIdentity(identityId);
    }

    public IReadOnlyList<GuardianRecord> GetGuardians()
    {
        return _store.GetAll();
    }

    public IReadOnlyList<GuardianRecord> GetGuardiansByRole(GuardianRole role)
    {
        return _store.GetByRole(role);
    }

    public IReadOnlyList<GuardianRecord> GetGuardiansByDomain(string domain)
    {
        return _store.GetByDomain(domain);
    }

    public void UpdateGuardianStatus(Guid guardianId, GuardianStatus status)
    {
        var existing = _store.GetById(guardianId)
            ?? throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        _store.Update(existing with { GuardianStatus = status });
    }
}
