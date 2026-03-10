namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("CapitalContribution", EngineTier.T2E, EngineKind.Mutation, "CapitalContributionRequest", typeof(EngineEvent))]
public sealed class CapitalContributionEngine : IEngine
{
    public string Name => "CapitalContribution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        if (!Guid.TryParse(spvId, out var spvGuid))
            return Task.FromResult(EngineResult.Fail("Invalid spvId format"));

        var vaultId = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultId))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));

        if (!Guid.TryParse(vaultId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing contribution amount"));

        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Contribution amount must be positive"));

        var purpose = context.Data.GetValueOrDefault("purpose") as string ?? "General";
        var capitalId = Guid.NewGuid();

        // Event compatible with whyce.spv.events and whyce.economic.events topics
        var events = new[]
        {
            EngineEvent.Create("CapitalContributed", spvGuid,
                new Dictionary<string, object>
                {
                    ["capitalId"] = capitalId.ToString(),
                    ["spvId"] = spvId,
                    ["vaultId"] = vaultId,
                    ["amount"] = amount.Value,
                    ["purpose"] = purpose,
                    ["topic"] = "whyce.spv.events"
                }),
            EngineEvent.Create("VaultDebited", Guid.Parse(vaultId),
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultId,
                    ["amount"] = amount.Value,
                    ["reason"] = $"Capital contribution to SPV {spvId}",
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["capitalId"] = capitalId.ToString(),
                ["spvId"] = spvId,
                ["vaultId"] = vaultId,
                ["amount"] = amount.Value,
                ["purpose"] = purpose
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
