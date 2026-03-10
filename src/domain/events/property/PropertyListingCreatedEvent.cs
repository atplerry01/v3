namespace Whycespace.Domain.Events.Property;

public sealed record PropertyListingCreatedEvent(
    Guid PropertyId,
    string Address,
    string ListingStatus,
    DateTimeOffset Timestamp
);
