namespace Whycespace.Engines.T3I_Intelligence;

using Whycespace.Contracts.Engines;

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
