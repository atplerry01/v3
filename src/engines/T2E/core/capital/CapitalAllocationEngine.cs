namespace Whycespace.Engines.T2E.Core.Capital;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalAllocation", EngineTier.T2E, EngineKind.Mutation, "AllocateCapitalCommand", typeof(EngineEvent))]
public sealed class CapitalAllocationEngine : IEngine
{
    public string Name => "CapitalAllocation";

    private static readonly string[] ValidTargetTypes = ["SPV", "Asset", "Project", "OperationalProgram"];
    private static readonly string[] ValidCurrencies = ["GBP", "USD", "EUR", "NGN"];

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string ?? "allocate";

        return action.ToLowerInvariant() switch
        {
            "allocate" => HandleAllocate(context),
            "cancel" => HandleCancel(context),
            "reassign" => HandleReassign(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action: {action}"))
        };
    }

    private Task<EngineResult> HandleAllocate(EngineContext context)
    {
        var reservationId = context.Data.GetValueOrDefault("reservationId") as string;
        if (string.IsNullOrEmpty(reservationId))
            return Task.FromResult(EngineResult.Fail("Missing reservationId"));
        if (!Guid.TryParse(reservationId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid reservationId format"));

        var poolId = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolId))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        var targetType = context.Data.GetValueOrDefault("targetType") as string;
        if (string.IsNullOrEmpty(targetType))
            return Task.FromResult(EngineResult.Fail("Missing targetType"));
        if (!ValidTargetTypes.Contains(targetType))
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
        if (!ValidCurrencies.Contains(currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}"));

        var allocatedBy = context.Data.GetValueOrDefault("allocatedBy") as string;
        if (string.IsNullOrEmpty(allocatedBy))
            return Task.FromResult(EngineResult.Fail("Missing allocatedBy"));

        var allocationId = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationId))
            allocationId = Guid.NewGuid().ToString();
        else if (!Guid.TryParse(allocationId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        var now = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalAllocated", Guid.Parse(allocationId),
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationId,
                    ["reservationId"] = reservationId,
                    ["poolId"] = poolId,
                    ["targetType"] = targetType,
                    ["targetId"] = targetId,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["allocatedBy"] = allocatedBy,
                    ["allocatedAt"] = now.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                }),
            EngineEvent.Create("ReservationConsumed", Guid.Parse(reservationId),
                new Dictionary<string, object>
                {
                    ["reservationId"] = reservationId,
                    ["allocationId"] = allocationId,
                    ["amount"] = amount.Value,
                    ["topic"] = "whyce.capital.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["allocationId"] = allocationId,
                ["reservationId"] = reservationId,
                ["poolId"] = poolId,
                ["targetType"] = targetType,
                ["targetId"] = targetId,
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["status"] = "Allocated",
                ["allocatedAt"] = now.ToString("O")
            }));
    }

    private Task<EngineResult> HandleCancel(EngineContext context)
    {
        var allocationId = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationId))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing cancellation reason"));

        var cancelledBy = context.Data.GetValueOrDefault("cancelledBy") as string;
        if (string.IsNullOrEmpty(cancelledBy))
            return Task.FromResult(EngineResult.Fail("Missing cancelledBy"));

        var now = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalAllocationCancelled", Guid.Parse(allocationId),
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationId,
                    ["reason"] = reason,
                    ["cancelledBy"] = cancelledBy,
                    ["cancelledAt"] = now.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["allocationId"] = allocationId,
                ["status"] = "Cancelled",
                ["reason"] = reason,
                ["cancelledAt"] = now.ToString("O")
            }));
    }

    private Task<EngineResult> HandleReassign(EngineContext context)
    {
        var allocationId = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationId))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        var newTargetType = context.Data.GetValueOrDefault("newTargetType") as string;
        if (string.IsNullOrEmpty(newTargetType))
            return Task.FromResult(EngineResult.Fail("Missing newTargetType"));
        if (!ValidTargetTypes.Contains(newTargetType))
            return Task.FromResult(EngineResult.Fail($"Invalid newTargetType: {newTargetType}. Must be one of: {string.Join(", ", ValidTargetTypes)}"));

        var newTargetId = context.Data.GetValueOrDefault("newTargetId") as string;
        if (string.IsNullOrEmpty(newTargetId))
            return Task.FromResult(EngineResult.Fail("Missing newTargetId"));
        if (!Guid.TryParse(newTargetId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid newTargetId format"));

        var reassignedBy = context.Data.GetValueOrDefault("reassignedBy") as string;
        if (string.IsNullOrEmpty(reassignedBy))
            return Task.FromResult(EngineResult.Fail("Missing reassignedBy"));

        var now = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalAllocationReassigned", Guid.Parse(allocationId),
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationId,
                    ["newTargetType"] = newTargetType,
                    ["newTargetId"] = newTargetId,
                    ["reassignedBy"] = reassignedBy,
                    ["reassignedAt"] = now.ToString("O"),
                    ["topic"] = "whyce.capital.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["allocationId"] = allocationId,
                ["newTargetType"] = newTargetType,
                ["newTargetId"] = newTargetId,
                ["status"] = "Reassigned",
                ["reassignedAt"] = now.ToString("O")
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
