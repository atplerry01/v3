namespace Whycespace.Domain.Cluster.Property;

public sealed record Tenant(
    Guid TenantId,
    string Name,
    Guid? CurrentListingId,
    DateTimeOffset RegisteredAt
);
