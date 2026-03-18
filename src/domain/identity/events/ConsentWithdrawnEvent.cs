namespace Whycespace.Domain.Identity.Events;

public sealed record ConsentWithdrawnEvent(
    Guid EventId,
    Guid IdentityId,
    string ConsentType,
    string Reason,
    DateTime WithdrawnAt,
    int EventVersion);
