namespace Whycespace.Domain.Identity.Events;

public sealed record FederationProviderRegisteredEvent(
    Guid EventId,
    string ProviderName,
    string ProviderType,
    DateTime RegisteredAt,
    int EventVersion);
