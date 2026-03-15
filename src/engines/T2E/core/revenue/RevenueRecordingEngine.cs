namespace Whycespace.Engines.T2E.Core.Revenue;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("RevenueRecording", EngineTier.T2E, EngineKind.Mutation, "RevenueRecordingRequest", typeof(EngineEvent))]
public sealed class RevenueRecordingEngine : IEngine
{
    public string Name => "RevenueRecording";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        if (!Guid.TryParse(spvId, out var spvGuid))
            return Task.FromResult(EngineResult.Fail("Invalid spvId format"));

        var assetId = context.Data.GetValueOrDefault("assetId") as string;
        if (string.IsNullOrEmpty(assetId))
            return Task.FromResult(EngineResult.Fail("Missing assetId"));

        if (!Guid.TryParse(assetId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid assetId format"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing revenue amount"));

        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Revenue amount must be positive"));

        var source = context.Data.GetValueOrDefault("source") as string;
        if (string.IsNullOrEmpty(source))
            return Task.FromResult(EngineResult.Fail("Missing revenue source"));

        var revenueId = Guid.NewGuid();
        var period = context.Data.GetValueOrDefault("period") as string
            ?? DateTimeOffset.UtcNow.ToString("yyyy-MM");

        // Event compatible with whyce.economic.events and whyce.spv.events topics
        var events = new[]
        {
            EngineEvent.Create("RevenueRecorded", spvGuid,
                new Dictionary<string, object>
                {
                    ["revenueId"] = revenueId.ToString(),
                    ["spvId"] = spvId,
                    ["assetId"] = assetId,
                    ["amount"] = amount.Value,
                    ["source"] = source,
                    ["period"] = period,
                    ["topic"] = "whyce.economic.events"
                }),
            EngineEvent.Create("SpvRevenueUpdated", spvGuid,
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId,
                    ["revenueId"] = revenueId.ToString(),
                    ["amount"] = amount.Value,
                    ["topic"] = "whyce.spv.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["revenueId"] = revenueId.ToString(),
                ["spvId"] = spvId,
                ["assetId"] = assetId,
                ["amount"] = amount.Value,
                ["source"] = source,
                ["period"] = period
            }));
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
