namespace Whycespace.Domain.Identity.Events;

public sealed record IdentityAttributeUpdatedEvent(
    Guid EventId,
    Guid IdentityId,
    string AttributeKey,
    string AttributeValue,
    Guid UpdatedBy,
    DateTime Timestamp,
    int EventVersion);
