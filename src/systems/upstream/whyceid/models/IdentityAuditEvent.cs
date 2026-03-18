namespace Whycespace.Systems.WhyceID.Models;

public sealed record IdentityAuditEvent(
    Guid EventId,
    Guid IdentityId,
    string EventType,
    string Description,
    DateTime Timestamp
);
