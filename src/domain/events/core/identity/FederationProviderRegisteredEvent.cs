namespace Whycespace.Domain.Events.Core.Identity;

public sealed record FederationProviderRegisteredEvent(
    Guid EventId,
    string ProviderName,
    string ProviderType,
    DateTime RegisteredAt,
    int EventVersion);
