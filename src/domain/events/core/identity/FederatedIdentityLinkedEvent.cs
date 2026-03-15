namespace Whycespace.Domain.Events.Core.Identity;

public sealed record FederatedIdentityLinkedEvent(
    Guid EventId,
    Guid IdentityId,
    string ProviderName,
    string ExternalIdentityId,
    DateTime LinkedAt,
    int EventVersion);
