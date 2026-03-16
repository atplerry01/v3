namespace Whycespace.Systems.Upstream.Governance.Registry;

using Whycespace.Systems.Upstream.Governance.Models;

public interface IGuardianRegistry
{
    void RegisterGuardian(GuardianRecord record);

    GuardianRecord? GetGuardian(Guid guardianId);

    GuardianRecord? GetGuardianByIdentity(string identityId);

    IReadOnlyList<GuardianRecord> GetGuardians();

    IReadOnlyList<GuardianRecord> GetGuardiansByRole(GuardianRole role);

    IReadOnlyList<GuardianRecord> GetGuardiansByDomain(string domain);

    void UpdateGuardianStatus(Guid guardianId, GuardianStatus status);
}
