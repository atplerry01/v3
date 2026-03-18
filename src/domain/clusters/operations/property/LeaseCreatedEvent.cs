namespace Whycespace.Domain.Events.Clusters.Property;

public sealed record LeaseCreatedEvent(
    Guid LeaseId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
