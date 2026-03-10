namespace Whycespace.Runtime.Projections;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Projections;

public sealed class PropertyListingProjection : IProjection
{
    private readonly Dictionary<string, Dictionary<string, object>> _listings = new();

    public string Name => "PropertyListing";

    public Task HandleAsync(SystemEvent @event)
    {
        if (@event.EventType == "ListingPublished")
        {
            var listingId = @event.AggregateId.ToString();
            _listings[listingId] = new Dictionary<string, object>(@event.Payload);
        }
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, Dictionary<string, object>> GetListings() => _listings;
}
