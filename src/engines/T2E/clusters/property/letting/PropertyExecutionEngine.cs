namespace Whycespace.Engines.T2E.Clusters.Property.Letting;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("PropertyExecution", EngineTier.T2E, EngineKind.Mutation, "PropertyExecutionRequest", typeof(EngineEvent))]
public sealed class PropertyExecutionEngine : IEngine
{
    public string Name => "PropertyExecution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var step = context.WorkflowStep;

        return step switch
        {
            "ValidateListing" => HandleValidateListing(context),
            "PublishListing" => HandlePublishListing(context),
            "MatchTenant" => HandleMatchTenant(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown step: {step}"))
        };
    }

    private static Task<EngineResult> HandleValidateListing(EngineContext context)
    {
        var hasTitle = context.Data.ContainsKey("title");
        var hasRent = context.Data.ContainsKey("monthlyRent");
        if (!hasTitle || !hasRent)
            return Task.FromResult(EngineResult.Fail("Missing listing details"));

        var events = new[] { EngineEvent.Create("ListingValidated", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandlePublishListing(EngineContext context)
    {
        var events = new[] { EngineEvent.Create("ListingPublished", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleMatchTenant(EngineContext context)
    {
        var tenantId = context.Data.GetValueOrDefault("tenantId") as string;
        if (string.IsNullOrEmpty(tenantId))
            return Task.FromResult(EngineResult.Fail("No tenant matched"));

        var events = new[] { EngineEvent.Create("TenantMatched", Guid.Parse(context.WorkflowId),
            new Dictionary<string, object> { ["tenantId"] = tenantId }) };
        return Task.FromResult(EngineResult.Ok(events));
    }
}
