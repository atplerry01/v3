namespace Whycespace.System.WhyceID.Registry;

using Whycespace.System.WhyceID.Models;

public interface IIdentityRegistry
{
    void RegisterIdentity(IdentityRecord record);

    IdentityRecord GetIdentity(Guid identityId);

    IdentityRecord? GetIdentityByEmail(string email);

    IdentityRecord? GetIdentityByPhone(string phone);

    void UpdateIdentityStatus(Guid identityId, IdentityStatus status);

    IReadOnlyList<IdentityRecord> ListIdentities(int page, int pageSize);
}
