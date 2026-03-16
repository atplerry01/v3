namespace Whycespace.Systems.Upstream.Governance.Stores;

using Whycespace.Systems.Upstream.Governance.Models;

public interface IGuardianRegistryStore
{
    void Save(GuardianRecord record);

    GuardianRecord? GetById(Guid guardianId);

    GuardianRecord? GetByIdentity(string identityId);

    IReadOnlyList<GuardianRecord> GetAll();

    IReadOnlyList<GuardianRecord> GetByRole(GuardianRole role);

    IReadOnlyList<GuardianRecord> GetByDomain(string domain);

    void Update(GuardianRecord record);

    bool ExistsById(Guid guardianId);

    bool ExistsByIdentity(string identityId);
}
