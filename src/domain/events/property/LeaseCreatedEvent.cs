namespace Whycespace.Domain.Events.Property;

public sealed record LeaseCreatedEvent(
    Guid LeaseId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
