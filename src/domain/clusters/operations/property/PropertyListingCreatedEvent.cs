namespace Whycespace.Domain.Clusters.Operations.Property;

public sealed record PropertyListingCreatedEvent(
    Guid PropertyId,
    string Address,
    string ListingStatus,
    DateTimeOffset Timestamp
);
