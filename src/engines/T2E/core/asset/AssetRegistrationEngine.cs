namespace Whycespace.Engines.T2E.Core.Asset;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("AssetRegistration", EngineTier.T2E, EngineKind.Mutation, "AssetRegistrationRequest", typeof(EngineEvent))]
public sealed class AssetRegistrationEngine : IEngine
{
    public string Name => "AssetRegistration";

    private static readonly string[] ValidAssetTypes =
        { "Vehicle", "Property", "Equipment", "Financial", "Intellectual", "Digital" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        if (!Guid.TryParse(spvId, out var spvGuid))
            return Task.FromResult(EngineResult.Fail("Invalid spvId format"));

        var assetType = context.Data.GetValueOrDefault("assetType") as string;
        if (string.IsNullOrEmpty(assetType))
            return Task.FromResult(EngineResult.Fail("Missing assetType"));

        if (!Array.Exists(ValidAssetTypes, t => t == assetType))
            return Task.FromResult(EngineResult.Fail($"Invalid assetType: {assetType}. Valid: {string.Join(", ", ValidAssetTypes)}"));

        var description = context.Data.GetValueOrDefault("description") as string;
        if (string.IsNullOrEmpty(description))
            return Task.FromResult(EngineResult.Fail("Missing asset description"));

        var value = ResolveDecimal(context.Data.GetValueOrDefault("value"));
        if (value is null)
            return Task.FromResult(EngineResult.Fail("Missing asset value"));

        if (value.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Asset value must be positive"));

        var assetId = Guid.NewGuid();

        // Event compatible with whyce.spv.events topic
        var events = new[]
        {
            EngineEvent.Create("AssetRegistered", spvGuid,
                new Dictionary<string, object>
                {
                    ["assetId"] = assetId.ToString(),
                    ["spvId"] = spvId,
                    ["assetType"] = assetType,
                    ["description"] = description,
                    ["value"] = value.Value,
                    ["topic"] = "whyce.spv.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["assetId"] = assetId.ToString(),
                ["spvId"] = spvId,
                ["assetType"] = assetType,
                ["description"] = description,
                ["value"] = value.Value
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
