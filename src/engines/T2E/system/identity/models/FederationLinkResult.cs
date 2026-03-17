namespace Whycespace.Engines.T2E.System.Identity.Models;

public sealed record FederationLinkResult(
    Guid IdentityId,
    string ProviderName,
    string ExternalIdentityId,
    bool Linked,
    DateTime LinkedAt);
