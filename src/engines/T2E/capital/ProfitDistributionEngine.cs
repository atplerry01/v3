namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("ProfitDistribution", EngineTier.T2E, EngineKind.Mutation, "ProfitDistributionRequest", typeof(EngineEvent))]
public sealed class ProfitDistributionEngine : IEngine
{
    public string Name => "ProfitDistribution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        if (!Guid.TryParse(spvId, out var spvGuid))
            return Task.FromResult(EngineResult.Fail("Invalid spvId format"));

        var vaultId = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultId))
            return Task.FromResult(EngineResult.Fail("Missing target vaultId"));

        if (!Guid.TryParse(vaultId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        var totalRevenue = ResolveDecimal(context.Data.GetValueOrDefault("totalRevenue"));
        if (totalRevenue is null)
            return Task.FromResult(EngineResult.Fail("Missing totalRevenue"));

        var totalCosts = ResolveDecimal(context.Data.GetValueOrDefault("totalCosts")) ?? 0m;
        if (totalCosts < 0)
            return Task.FromResult(EngineResult.Fail("totalCosts cannot be negative"));

        var netProfit = totalRevenue.Value - totalCosts;
        if (netProfit <= 0)
            return Task.FromResult(EngineResult.Fail($"No distributable profit. Net: {netProfit} (revenue: {totalRevenue.Value}, costs: {totalCosts})"));

        var distributionRate = ResolveDecimal(context.Data.GetValueOrDefault("distributionRate")) ?? 1.0m;
        if (distributionRate <= 0 || distributionRate > 1.0m)
            return Task.FromResult(EngineResult.Fail("distributionRate must be between 0 (exclusive) and 1 (inclusive)"));

        var distributionAmount = Math.Round(netProfit * distributionRate, 2);
        var retainedAmount = netProfit - distributionAmount;
        var distributionId = Guid.NewGuid();
        var period = context.Data.GetValueOrDefault("period") as string
            ?? DateTimeOffset.UtcNow.ToString("yyyy-MM");

        // Events compatible with whyce.economic.events and whyce.spv.events topics
        var events = new[]
        {
            EngineEvent.Create("ProfitDistributed", spvGuid,
                new Dictionary<string, object>
                {
                    ["distributionId"] = distributionId.ToString(),
                    ["spvId"] = spvId,
                    ["vaultId"] = vaultId,
                    ["totalRevenue"] = totalRevenue.Value,
                    ["totalCosts"] = totalCosts,
                    ["netProfit"] = netProfit,
                    ["distributionRate"] = distributionRate,
                    ["distributionAmount"] = distributionAmount,
                    ["retainedAmount"] = retainedAmount,
                    ["period"] = period,
                    ["topic"] = "whyce.economic.events"
                }),
            EngineEvent.Create("VaultCredited", Guid.Parse(vaultId),
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultId,
                    ["amount"] = distributionAmount,
                    ["reason"] = $"Profit distribution from SPV {spvId} for period {period}",
                    ["topic"] = "whyce.economic.events"
                }),
            EngineEvent.Create("SpvProfitSettled", spvGuid,
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId,
                    ["distributionId"] = distributionId.ToString(),
                    ["distributed"] = distributionAmount,
                    ["retained"] = retainedAmount,
                    ["topic"] = "whyce.spv.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["distributionId"] = distributionId.ToString(),
                ["spvId"] = spvId,
                ["vaultId"] = vaultId,
                ["netProfit"] = netProfit,
                ["distributionAmount"] = distributionAmount,
                ["retainedAmount"] = retainedAmount,
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
