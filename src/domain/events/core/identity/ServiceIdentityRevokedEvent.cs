namespace Whycespace.Domain.Events.Core.Identity;

public sealed record ServiceIdentityRevokedEvent(
    Guid EventId,
    Guid ServiceIdentityId,
    string Reason,
    DateTime RevokedAt,
    int EventVersion);
