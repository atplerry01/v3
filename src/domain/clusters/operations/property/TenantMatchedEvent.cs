namespace Whycespace.Domain.Clusters.Operations.Property;

public sealed record TenantMatchedEvent(
    Guid MatchId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
