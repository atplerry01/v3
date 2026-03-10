namespace Whycespace.Engines.T3I_Intelligence;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("PropertyListing", EngineTier.T3I, EngineKind.Decision, "PropertyListingRequest", typeof(EngineEvent))]
public sealed class PropertyListingEngine : IEngine
{
    public string Name => "PropertyListing";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var address = context.Data.GetValueOrDefault("address") as string;
        if (string.IsNullOrEmpty(address))
            return Task.FromResult(EngineResult.Fail("Missing address"));

        var ownerId = context.Data.GetValueOrDefault("ownerId") as string;
        if (string.IsNullOrEmpty(ownerId))
            return Task.FromResult(EngineResult.Fail("Missing ownerId"));

        var propertyId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("PropertyListingCreated", propertyId,
                new Dictionary<string, object>
                {
                    ["propertyId"] = propertyId.ToString(),
                    ["address"] = address,
                    ["ownerId"] = ownerId,
                    ["listingStatus"] = "Available",
                    ["topic"] = "whyce.property.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["propertyId"] = propertyId.ToString(),
                ["address"] = address,
                ["listingStatus"] = "Available"
            }));
    }
}
