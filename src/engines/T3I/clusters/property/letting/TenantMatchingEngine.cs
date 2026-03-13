namespace Whycespace.Engines.T3I.Clusters.Property.Letting;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("TenantMatching", EngineTier.T3I, EngineKind.Decision, "TenantMatchingRequest", typeof(EngineEvent))]
public sealed class TenantMatchingEngine : IEngine
{
    public string Name => "TenantMatching";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var listingId = context.Data.GetValueOrDefault("listingId") as string;
        if (string.IsNullOrEmpty(listingId))
            return Task.FromResult(EngineResult.Fail("Missing listingId"));

        var matchedTenantId = Guid.NewGuid().ToString();

        var events = new[]
        {
            EngineEvent.Create("TenantMatched", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["matchedTenantId"] = matchedTenantId,
                    ["listingId"] = listingId
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["tenantId"] = matchedTenantId }));
    }
}
