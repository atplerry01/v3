namespace Whycespace.Systems.WhyceID.Registry;

using Whycespace.Systems.WhyceID.Models;

public interface IIdentityRegistry
{
    void RegisterIdentity(IdentityRecord record);

    IdentityRecord GetIdentity(Guid identityId);

    IdentityRecord? GetIdentityByEmail(string email);

    IdentityRecord? GetIdentityByPhone(string phone);

    void UpdateIdentityStatus(Guid identityId, IdentityStatus status);

    IReadOnlyList<IdentityRecord> ListIdentities(int page, int pageSize);
}
