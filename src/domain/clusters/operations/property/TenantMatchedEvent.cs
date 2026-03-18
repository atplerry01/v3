namespace Whycespace.Domain.Events.Clusters.Property;

public sealed record TenantMatchedEvent(
    Guid MatchId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
