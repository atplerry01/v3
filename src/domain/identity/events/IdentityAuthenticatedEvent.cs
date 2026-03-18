namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityAuthenticatedEvent(
    Guid EventId,
    Guid IdentityId,
    string AuthenticationMethod,
    string DeviceId,
    DateTime AuthenticatedAt,
    int EventVersion);
