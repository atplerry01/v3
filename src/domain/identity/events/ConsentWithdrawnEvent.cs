namespace Whycespace.Domain.Events.Core.Identity;

public sealed record ConsentWithdrawnEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    string Reason,
    DateTime WithdrawnAt,
    int EventVersion);
