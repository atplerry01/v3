namespace Whycespace.Domain.Events.Clusters.Property;

public sealed record PropertyListingCreatedEvent(
    Guid PropertyId,
    string Address,
    string ListingStatus,
    DateTimeOffset Timestamp
);
