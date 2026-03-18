namespace Whycespace.Engines.T2E.Economic.Capital.Utilization.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalUtilization", EngineTier.T2E, EngineKind.Mutation, "UtilizeCapitalCommand", typeof(EngineEvent))]
public sealed class CapitalUtilizationEngine : IEngine
{
    private static readonly string[] ValidTargetTypes = ["SPV", "Asset", "Project", "OperationalProgram"];
    private static readonly string[] ValidCurrencies = ["GBP", "USD", "EUR", "NGN"];

    public string Name => "CapitalUtilization";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var operation = context.Data.GetValueOrDefault("operation") as string ?? context.WorkflowStep;

        return operation switch
        {
            "UtilizeCapital" => ExecuteUtilize(context),
            "AdjustUtilization" => ExecuteAdjust(context),
            "ReverseUtilization" => ExecuteReverse(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown operation: {operation}"))
        };
    }

    private Task<EngineResult> ExecuteUtilize(EngineContext context)
    {
        var utilizationId = context.Data.GetValueOrDefault("utilizationId") as string;
        if (string.IsNullOrEmpty(utilizationId))
            return Task.FromResult(EngineResult.Fail("Missing utilizationId"));
        if (!Guid.TryParse(utilizationId, out var utilizationGuid))
            return Task.FromResult(EngineResult.Fail("Invalid utilizationId format"));

        var allocationId = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationId))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        var poolId = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolId))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        var targetType = context.Data.GetValueOrDefault("targetType") as string;
        if (string.IsNullOrEmpty(targetType))
            return Task.FromResult(EngineResult.Fail("Missing targetType"));
        if (!Array.Exists(ValidTargetTypes, t => t == targetType))
            return Task.FromResult(EngineResult.Fail($"Invalid targetType: {targetType}. Must be one of: {string.Join(", ", ValidTargetTypes)}"));

        var targetId = context.Data.GetValueOrDefault("targetId") as string;
        if (string.IsNullOrEmpty(targetId))
            return Task.FromResult(EngineResult.Fail("Missing targetId"));
        if (!Guid.TryParse(targetId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid targetId format"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Amount must be positive"));

        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(ValidCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}"));

        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";
        var utilizedBy = context.Data.GetValueOrDefault("utilizedBy") as string;
        if (string.IsNullOrEmpty(utilizedBy))
            return Task.FromResult(EngineResult.Fail("Missing utilizedBy"));

        var utilizedAt = ResolveDateTime(context.Data.GetValueOrDefault("utilizedAt")) ?? DateTimeOffset.UtcNow;

        var events = new List<EngineEvent>
        {
            EngineEvent.Create("CapitalUtilized", utilizationGuid,
                new Dictionary<string, object>
                {
                    ["utilizationId"] = utilizationId,
                    ["allocationId"] = allocationId,
                    ["poolId"] = poolId,
                    ["targetType"] = targetType,
                    ["targetId"] = targetId,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["referenceId"] = referenceId,
                    ["utilizedBy"] = utilizedBy,
                    ["utilizedAt"] = utilizedAt.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                }),
            EngineEvent.Create("AllocationConsumed", Guid.Parse(allocationId),
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationId,
                    ["utilizationId"] = utilizationId,
                    ["amount"] = amount.Value,
                    ["topic"] = "whyce.capital.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["utilizationId"] = utilizationId,
            ["allocationId"] = allocationId,
            ["poolId"] = poolId,
            ["targetType"] = targetType,
            ["targetId"] = targetId,
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["status"] = "Utilized",
            ["timestamp"] = utilizedAt.ToString("O"),
            ["message"] = "Capital utilization recorded"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ExecuteAdjust(EngineContext context)
    {
        var utilizationId = context.Data.GetValueOrDefault("utilizationId") as string;
        if (string.IsNullOrEmpty(utilizationId))
            return Task.FromResult(EngineResult.Fail("Missing utilizationId"));
        if (!Guid.TryParse(utilizationId, out var utilizationGuid))
            return Task.FromResult(EngineResult.Fail("Invalid utilizationId format"));

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
            EngineEvent.Create("CapitalUtilizationAdjusted", utilizationGuid,
                new Dictionary<string, object>
                {
                    ["utilizationId"] = utilizationId,
                    ["adjustmentAmount"] = adjustmentAmount.Value,
                    ["reason"] = reason,
                    ["adjustedBy"] = adjustedBy,
                    ["adjustedAt"] = adjustedAt.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["utilizationId"] = utilizationId,
            ["adjustmentAmount"] = adjustmentAmount.Value,
            ["status"] = "Adjusted",
            ["timestamp"] = adjustedAt.ToString("O"),
            ["message"] = $"Capital utilization adjusted: {reason}"
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ExecuteReverse(EngineContext context)
    {
        var utilizationId = context.Data.GetValueOrDefault("utilizationId") as string;
        if (string.IsNullOrEmpty(utilizationId))
            return Task.FromResult(EngineResult.Fail("Missing utilizationId"));
        if (!Guid.TryParse(utilizationId, out var utilizationGuid))
            return Task.FromResult(EngineResult.Fail("Invalid utilizationId format"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing reason for reversal"));

        var reversedBy = context.Data.GetValueOrDefault("reversedBy") as string;
        if (string.IsNullOrEmpty(reversedBy))
            return Task.FromResult(EngineResult.Fail("Missing reversedBy"));

        var reversedAt = ResolveDateTime(context.Data.GetValueOrDefault("reversedAt")) ?? DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalUtilizationReversed", utilizationGuid,
                new Dictionary<string, object>
                {
                    ["utilizationId"] = utilizationId,
                    ["reason"] = reason,
                    ["reversedBy"] = reversedBy,
                    ["reversedAt"] = reversedAt.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["utilizationId"] = utilizationId,
            ["status"] = "Reversed",
            ["timestamp"] = reversedAt.ToString("O"),
            ["message"] = $"Capital utilization reversed: {reason}"
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