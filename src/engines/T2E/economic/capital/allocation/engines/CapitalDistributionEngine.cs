namespace Whycespace.Engines.T2E.Economic.Capital.Allocation.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalDistribution", EngineTier.T2E, EngineKind.Mutation, "DistributeCapitalCommand", typeof(EngineEvent))]
public sealed class CapitalDistributionEngine : IEngine
{
    private static readonly string[] SupportedCurrencies = ["GBP", "USD", "EUR", "NGN"];
    private static readonly string[] SupportedTargetTypes = ["Pool", "SPV", "CWG", "Investor"];
    private static readonly string[] SupportedDistributionTypes =
        ["ProfitDistribution", "ReturnOfCapital", "DividendDistribution", "LiquidationDistribution"];

    public string Name => "CapitalDistribution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string ?? context.WorkflowStep;

        return operation switch
        {
            "DistributeCapital" => ExecuteDistribute(context),
            "AdjustDistribution" => ExecuteAdjust(context),
            "ReverseDistribution" => ExecuteReverse(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> ExecuteDistribute(EngineContext context)
    {
        var distributionId = context.Data.GetValueOrDefault("distributionId") as string;
        if (string.IsNullOrEmpty(distributionId))
            return Task.FromResult(EngineResult.Fail("Missing distributionId"));
        if (!Guid.TryParse(distributionId, out var distributionGuid))
            return Task.FromResult(EngineResult.Fail("Invalid distributionId format"));

        var poolId = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolId))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolId, out var poolGuid))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        var targetType = context.Data.GetValueOrDefault("targetType") as string;
        if (string.IsNullOrEmpty(targetType))
            return Task.FromResult(EngineResult.Fail("Missing targetType"));
        if (!Array.Exists(SupportedTargetTypes, t => t == targetType))
            return Task.FromResult(EngineResult.Fail($"Invalid targetType: {targetType}. Supported: {string.Join(", ", SupportedTargetTypes)}"));

        var targetId = context.Data.GetValueOrDefault("targetId") as string;
        if (string.IsNullOrEmpty(targetId))
            return Task.FromResult(EngineResult.Fail("Missing targetId"));
        if (!Guid.TryParse(targetId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid targetId format"));

        var totalAmount = ResolveDecimal(context.Data.GetValueOrDefault("totalAmount"));
        if (totalAmount is null)
            return Task.FromResult(EngineResult.Fail("Missing totalAmount"));
        if (totalAmount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Total amount must be positive"));

        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}"));

        var distributionType = context.Data.GetValueOrDefault("distributionType") as string;
        if (string.IsNullOrEmpty(distributionType))
            return Task.FromResult(EngineResult.Fail("Missing distributionType"));
        if (!Array.Exists(SupportedDistributionTypes, d => d == distributionType))
            return Task.FromResult(EngineResult.Fail($"Invalid distributionType: {distributionType}. Supported: {string.Join(", ", SupportedDistributionTypes)}"));

        var distributedBy = context.Data.GetValueOrDefault("distributedBy") as string;
        if (string.IsNullOrEmpty(distributedBy))
            return Task.FromResult(EngineResult.Fail("Missing distributedBy"));

        var distributedAt = ResolveDateTime(context.Data.GetValueOrDefault("distributedAt")) ?? DateTimeOffset.UtcNow;

        var events = new List<EngineEvent>
        {
            EngineEvent.Create("CapitalDistributed", poolGuid,
                new Dictionary<string, object>
                {
                    ["distributionId"] = distributionId,
                    ["poolId"] = poolId,
                    ["targetType"] = targetType,
                    ["targetId"] = targetId,
                    ["totalAmount"] = totalAmount.Value,
                    ["currency"] = currency,
                    ["distributionType"] = distributionType,
                    ["distributedBy"] = distributedBy,
                    ["distributedAt"] = distributedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["distributionId"] = distributionId,
            ["poolId"] = poolId,
            ["targetType"] = targetType,
            ["targetId"] = targetId,
            ["totalAmount"] = totalAmount.Value,
            ["currency"] = currency,
            ["status"] = "Distributed",
            ["timestamp"] = distributedAt.ToString("O"),
            ["message"] = $"Capital distributed: {distributionType}"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ExecuteAdjust(EngineContext context)
    {
        var distributionId = context.Data.GetValueOrDefault("distributionId") as string;
        if (string.IsNullOrEmpty(distributionId))
            return Task.FromResult(EngineResult.Fail("Missing distributionId"));
        if (!Guid.TryParse(distributionId, out var distributionGuid))
            return Task.FromResult(EngineResult.Fail("Invalid distributionId format"));

        var adjustmentAmount = ResolveDecimal(context.Data.GetValueOrDefault("adjustmentAmount"));
        if (adjustmentAmount is null)
            return Task.FromResult(EngineResult.Fail("Missing adjustmentAmount"));
        if (adjustmentAmount.Value == 0)
            return Task.FromResult(EngineResult.Fail("Adjustment amount cannot be zero"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason for adjustment"));

        var adjustedBy = context.Data.GetValueOrDefault("adjustedBy") as string;
        if (string.IsNullOrEmpty(adjustedBy))
            return Task.FromResult(EngineResult.Fail("Missing adjustedBy"));

        var adjustedAt = ResolveDateTime(context.Data.GetValueOrDefault("adjustedAt")) ?? DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalDistributionAdjusted", distributionGuid,
                new Dictionary<string, object>
                {
                    ["distributionId"] = distributionId,
                    ["adjustmentAmount"] = adjustmentAmount.Value,
                    ["reason"] = reason,
                    ["adjustedBy"] = adjustedBy,
                    ["adjustedAt"] = adjustedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["distributionId"] = distributionId,
            ["adjustmentAmount"] = adjustmentAmount.Value,
            ["status"] = "Adjusted",
            ["timestamp"] = adjustedAt.ToString("O"),
            ["message"] = $"Capital distribution adjusted: {reason}"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ExecuteReverse(EngineContext context)
    {
        var distributionId = context.Data.GetValueOrDefault("distributionId") as string;
        if (string.IsNullOrEmpty(distributionId))
            return Task.FromResult(EngineResult.Fail("Missing distributionId"));
        if (!Guid.TryParse(distributionId, out var distributionGuid))
            return Task.FromResult(EngineResult.Fail("Invalid distributionId format"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason for reversal"));

        var reversedBy = context.Data.GetValueOrDefault("reversedBy") as string;
        if (string.IsNullOrEmpty(reversedBy))
            return Task.FromResult(EngineResult.Fail("Missing reversedBy"));

        var reversedAt = ResolveDateTime(context.Data.GetValueOrDefault("reversedAt")) ?? DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalDistributionReversed", distributionGuid,
                new Dictionary<string, object>
                {
                    ["distributionId"] = distributionId,
                    ["reason"] = reason,
                    ["reversedBy"] = reversedBy,
                    ["reversedAt"] = reversedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["distributionId"] = distributionId,
            ["status"] = "Reversed",
            ["timestamp"] = reversedAt.ToString("O"),
            ["message"] = $"Capital distribution reversed: {reason}"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
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

    private static DateTimeOffset? ResolveDateTime(object? value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            string s when DateTimeOffset.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
