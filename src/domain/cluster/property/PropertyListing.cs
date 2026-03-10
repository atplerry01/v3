namespace Whycespace.Domain.Cluster.Property;

using Whycespace.Shared.Location;

public sealed record PropertyListing(
    Guid ListingId,
    Guid OwnerId,
    string Title,
    string Description,
    GeoLocation Location,
    decimal MonthlyRent,
    PropertyStatus Status,
    DateTimeOffset ListedAt
);

public enum PropertyStatus
{
    Available,
    UnderOffer,
    Let,
    Withdrawn
}
