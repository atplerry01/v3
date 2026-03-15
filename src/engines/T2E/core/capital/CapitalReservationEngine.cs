namespace Whycespace.Engines.T2E.Core.Capital;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalReservation", EngineTier.T2E, EngineKind.Mutation, "CapitalReservationRequest", typeof(EngineEvent))]
public sealed class CapitalReservationEngine : IEngine
{
    private static readonly string[] ValidTargetTypes = ["SPV", "Asset", "Project", "OperationalUse"];
    private static readonly string[] ValidCurrencies = ["GBP", "USD", "EUR"];

    public string Name => "CapitalReservation";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = context.Data.GetValueOrDefault("command") as string ?? context.WorkflowStep;

        return command switch
        {
            "ReserveCapital" => ExecuteReserve(context),
            "ReleaseReservation" => ExecuteRelease(context),
            "ExpireReservation" => ExecuteExpire(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown command: {command}"))
        };
    }

    private Task<EngineResult> ExecuteReserve(EngineContext context)
    {
        var poolId = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolId))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolId, out var poolGuid))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        var targetType = context.Data.GetValueOrDefault("targetType") as string;
        if (string.IsNullOrEmpty(targetType))
            return Task.FromResult(EngineResult.Fail("Missing targetType"));
        if (!Array.Exists(ValidTargetTypes, t => t == targetType))
            return Task.FromResult(EngineResult.Fail($"Invalid targetType. Must be one of: {string.Join(", ", ValidTargetTypes)}"));

        var targetId = context.Data.GetValueOrDefault("targetId") as string;
        if (string.IsNullOrEmpty(targetId))
            return Task.FromResult(EngineResult.Fail("Missing targetId"));
        if (!Guid.TryParse(targetId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid targetId format"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing reservation amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Reservation amount must be positive"));

        var currency = context.Data.GetValueOrDefault("currency") as string ?? "GBP";
        if (!Array.Exists(ValidCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Invalid currency. Must be one of: {string.Join(", ", ValidCurrencies)}"));

        var reservedBy = context.Data.GetValueOrDefault("reservedBy") as string;
        if (string.IsNullOrEmpty(reservedBy))
            return Task.FromResult(EngineResult.Fail("Missing reservedBy"));

        var reservationId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalReserved", poolGuid,
                new Dictionary<string, object>
                {
                    ["reservationId"] = reservationId.ToString(),
                    ["poolId"] = poolId,
                    ["targetType"] = targetType,
                    ["targetId"] = targetId,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["reservedBy"] = reservedBy,
                    ["topic"] = "whyce.economic.events"
                }),
            EngineEvent.Create("PoolBalanceReduced", poolGuid,
                new Dictionary<string, object>
                {
                    ["poolId"] = poolId,
                    ["amount"] = amount.Value,
                    ["reason"] = $"Capital reservation {reservationId} for {targetType} {targetId}",
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["reservationId"] = reservationId.ToString(),
                ["poolId"] = poolId,
                ["targetType"] = targetType,
                ["targetId"] = targetId,
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["status"] = "Reserved",
                ["timestamp"] = timestamp.ToString("O")
            }));
    }

    private Task<EngineResult> ExecuteRelease(EngineContext context)
    {
        var reservationId = context.Data.GetValueOrDefault("reservationId") as string;
        if (string.IsNullOrEmpty(reservationId))
            return Task.FromResult(EngineResult.Fail("Missing reservationId"));
        if (!Guid.TryParse(reservationId, out var reservationGuid))
            return Task.FromResult(EngineResult.Fail("Invalid reservationId format"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing release reason"));

        var releasedBy = context.Data.GetValueOrDefault("releasedBy") as string;
        if (string.IsNullOrEmpty(releasedBy))
            return Task.FromResult(EngineResult.Fail("Missing releasedBy"));

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalReservationReleased", reservationGuid,
                new Dictionary<string, object>
                {
                    ["reservationId"] = reservationId,
                    ["reason"] = reason,
                    ["releasedBy"] = releasedBy,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["reservationId"] = reservationId,
                ["status"] = "Released",
                ["reason"] = reason,
                ["timestamp"] = timestamp.ToString("O")
            }));
    }

    private Task<EngineResult> ExecuteExpire(EngineContext context)
    {
        var reservationId = context.Data.GetValueOrDefault("reservationId") as string;
        if (string.IsNullOrEmpty(reservationId))
            return Task.FromResult(EngineResult.Fail("Missing reservationId"));
        if (!Guid.TryParse(reservationId, out var reservationGuid))
            return Task.FromResult(EngineResult.Fail("Invalid reservationId format"));

        var expirationReason = context.Data.GetValueOrDefault("expirationReason") as string ?? "Reservation expired";

        var timestamp = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalReservationExpired", reservationGuid,
                new Dictionary<string, object>
                {
                    ["reservationId"] = reservationId,
                    ["expirationReason"] = expirationReason,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["reservationId"] = reservationId,
                ["status"] = "Expired",
                ["expirationReason"] = expirationReason,
                ["timestamp"] = timestamp.ToString("O")
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
