namespace Whycespace.Domain.Identity.Events;

public sealed record FederationLinkRevokedEvent(
    Guid EventId,
    Guid IdentityId,
    string ProviderName,
    DateTime RevokedAt,
    int EventVersion);
