namespace Whycespace.Domain.Events.Core.Identity;

public sealed record IdentityAuthenticatedEvent(
    Guid EventId,
    Guid IdentityId,
    string AuthenticationMethod,
    string DeviceId,
    DateTime AuthenticatedAt,
    int EventVersion);
