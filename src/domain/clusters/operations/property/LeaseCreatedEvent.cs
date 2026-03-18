namespace Whycespace.Domain.Clusters.Operations.Property;

public sealed record LeaseCreatedEvent(
    Guid LeaseId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
