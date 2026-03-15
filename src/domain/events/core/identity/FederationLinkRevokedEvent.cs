namespace Whycespace.Domain.Events.Core.Identity;

public sealed record FederationLinkRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string ProviderName,
    DateTime RevokedAt,
    int EventVersion);
