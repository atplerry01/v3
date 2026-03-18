namespace Whycespace.Domain.Clusters.Property.Letting;

public sealed record Tenant(
    Guid TenantId,
    string Name,
    Guid? CurrentListingId,
    DateTimeOffset RegisteredAt
);
