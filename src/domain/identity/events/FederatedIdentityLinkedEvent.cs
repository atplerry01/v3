namespace Whycespace.Domain.Identity.Events;

public sealed record FederatedIdentityLinkedEvent(
    Guid EventId,
    Guid IdentityId,
    string ProviderName,
    string ExternalIdentityId,
    DateTime LinkedAt,
    int EventVersion);
