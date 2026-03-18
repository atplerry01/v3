namespace Whycespace.Domain.Events.Core.Identity;

public sealed record ServiceIdentityRegisteredEvent(
    Guid EventId,
    Guid ServiceIdentityId,
    string ServiceName,
    string ServiceType,
    DateTime CreatedAt,
    int EventVersion);
