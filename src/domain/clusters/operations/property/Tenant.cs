namespace Whycespace.Domain.Clusters.Operations.Property;

public sealed record Tenant(
    Guid TenantId,
    string Name,
    Guid? CurrentListingId,
    DateTimeOffset RegisteredAt
);
