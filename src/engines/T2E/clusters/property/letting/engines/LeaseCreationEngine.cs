namespace Whycespace.Engines.T2E.Clusters.Property.Letting.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("LeaseCreation", EngineTier.T3I, EngineKind.Decision, "LeaseCreationRequest", typeof(EngineEvent))]
public sealed class LeaseCreationEngine : IEngine
{
    public string Name => "LeaseCreation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var tenantId = context.Data.GetValueOrDefault("tenantId") as string;
        if (string.IsNullOrEmpty(tenantId))
            return Task.FromResult(EngineResult.Fail("Missing tenantId"));

        if (!Guid.TryParse(tenantId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid tenantId format"));

        var propertyId = context.Data.GetValueOrDefault("propertyId") as string;
        if (string.IsNullOrEmpty(propertyId))
            return Task.FromResult(EngineResult.Fail("Missing propertyId"));

        if (!Guid.TryParse(propertyId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid propertyId format"));

        var leaseId = Guid.NewGuid();

        var events = new[]
        {
            EngineEvent.Create("LeaseCreated", leaseId,
                new Dictionary<string, object>
                {
                    ["leaseId"] = leaseId.ToString(),
                    ["tenantId"] = tenantId,
                    ["propertyId"] = propertyId,
                    ["topic"] = "whyce.property.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["leaseId"] = leaseId.ToString(),
                ["tenantId"] = tenantId,
                ["propertyId"] = propertyId
            }));
    }
}
