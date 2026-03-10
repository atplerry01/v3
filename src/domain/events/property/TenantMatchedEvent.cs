namespace Whycespace.Domain.Events.Property;

public sealed record TenantMatchedEvent(
    Guid MatchId,
    Guid TenantId,
    Guid PropertyId,
    DateTimeOffset Timestamp
);
