
namespace Whycespace.Simulation;

using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Domain.Economic.Events;
using Whycespace.Contracts.Events;

/// <summary>
/// Generates simulated EventEnvelopes for all Atlas projection types:
/// economic (capital, revenue, profit), identity, and workforce.
/// </summary>
public static class SimulationEventGenerator
{
    private static readonly ThreadLocal<Random> Rng = new(
        () => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));

    public static EventEnvelope GenerateCapitalContribution(Guid? spvId = null, decimal? amount = null)
    {
        var r = Rng.Value!;
        var spv = spvId ?? Guid.NewGuid();
        var evt = CapitalContributionRecordedEvent.Create(spv, amount ?? (1000m + r.Next(100000)));

        return Wrap("whyce.economic.capital-contribution-recorded", "whyce.economic.capital", evt, spv.ToString());
    }

    public static EventEnvelope GenerateCapitalDistribution(Guid? poolId = null, decimal? amount = null)
    {
        var r = Rng.Value!;
        var pool = poolId ?? Guid.NewGuid();
        var evt = CapitalDistributedEvent.Create(
            pool, "SPV", Guid.NewGuid(), amount ?? (500m + r.Next(50000)), "GBP", "Standard");

        return Wrap("whyce.economic.capital-distributed", "whyce.economic.capital", evt, pool.ToString());
    }

    public static EventEnvelope GenerateCapitalReserved(Guid? poolId = null, decimal? amount = null)
    {
        var r = Rng.Value!;
        var pool = poolId ?? Guid.NewGuid();
        var evt = CapitalReservedEvent.Create(
            Guid.NewGuid(), pool, "SPV", Guid.NewGuid(), amount ?? (200m + r.Next(20000)), "GBP");

        return Wrap("whyce.economic.capital-reserved", "whyce.economic.capital", evt, pool.ToString());
    }

    public static EventEnvelope GenerateRevenueRecorded(Guid? spvId = null, decimal? amount = null)
    {
        var r = Rng.Value!;
        var spv = spvId ?? Guid.NewGuid();
        var evt = RevenueRecordedEvent.Create(spv, amount ?? (100m + r.Next(10000)));

        return Wrap("whyce.economic.revenue-recorded", "whyce.economic.revenue", evt, spv.ToString());
    }

    public static EventEnvelope GenerateProfitDistributed(Guid? spvId = null, decimal? amount = null)
    {
        var r = Rng.Value!;
        var spv = spvId ?? Guid.NewGuid();
        var evt = ProfitDistributedEvent.Create(spv, Guid.NewGuid(), amount ?? (50m + r.Next(5000)));

        return Wrap("whyce.economic.profit-distributed", "whyce.economic.revenue", evt, spv.ToString());
    }

    public static EventEnvelope GenerateIdentityRegistered(Guid? identityId = null)
    {
        var id = identityId ?? Guid.NewGuid();
        var payload = new Dictionary<string, object>
        {
            ["IdentityId"] = id,
            ["DisplayName"] = $"User-{id.ToString("N")[..8]}",
            ["Email"] = $"user-{id.ToString("N")[..8]}@whycespace.sim"
        };

        return Wrap("whyce.identity.registered", "whyce.identity", payload, id.ToString());
    }

    public static EventEnvelope GenerateIdentityActivated(Guid identityId)
    {
        var payload = new Dictionary<string, object>
        {
            ["IdentityId"] = identityId
        };

        return Wrap("whyce.identity.activated", "whyce.identity", payload, identityId.ToString());
    }

    public static EventEnvelope GenerateIdentityRoleAssigned(Guid identityId, string role)
    {
        var payload = new Dictionary<string, object>
        {
            ["IdentityId"] = identityId,
            ["Role"] = role
        };

        return Wrap("whyce.identity.role-assigned", "whyce.identity", payload, identityId.ToString());
    }

    public static EventEnvelope GenerateTaskAssigned(Guid? workerId = null)
    {
        var wid = workerId ?? Guid.NewGuid();
        var payload = new Dictionary<string, object>
        {
            ["WorkerId"] = wid,
            ["TaskId"] = Guid.NewGuid()
        };

        return Wrap("whyce.heos.task-assigned", "whyce.heos.workforce", payload, wid.ToString());
    }

    public static EventEnvelope GenerateTaskCompleted(Guid workerId)
    {
        var payload = new Dictionary<string, object>
        {
            ["WorkerId"] = workerId,
            ["TaskId"] = Guid.NewGuid()
        };

        return Wrap("whyce.heos.task-completed", "whyce.heos.workforce", payload, workerId.ToString());
    }

    /// <summary>
    /// Generates a random mix of economic events for a given SPV, simulating a capital lifecycle.
    /// </summary>
    public static IReadOnlyList<EventEnvelope> GenerateEconomicLifecycle(Guid? spvId = null, int eventCount = 6)
    {
        var r = Rng.Value!;
        var spv = spvId ?? Guid.NewGuid();
        var events = new List<EventEnvelope>();

        // Always start with a contribution
        events.Add(GenerateCapitalContribution(spv, 50000m + r.Next(100000)));

        for (var i = 1; i < eventCount; i++)
        {
            var kind = r.Next(5);
            events.Add(kind switch
            {
                0 => GenerateCapitalContribution(spv),
                1 => GenerateCapitalDistribution(spv),
                2 => GenerateCapitalReserved(spv),
                3 => GenerateRevenueRecorded(spv),
                _ => GenerateProfitDistributed(spv)
            });
        }

        return events;
    }

    /// <summary>
    /// Generates a random event of any supported type.
    /// </summary>
    public static EventEnvelope GenerateRandom()
    {
        var r = Rng.Value!;
        return r.Next(8) switch
        {
            0 => GenerateCapitalContribution(),
            1 => GenerateCapitalDistribution(),
            2 => GenerateCapitalReserved(),
            3 => GenerateRevenueRecorded(),
            4 => GenerateProfitDistributed(),
            5 => GenerateIdentityRegistered(),
            6 => GenerateTaskAssigned(),
            _ => GenerateTaskAssigned()
        };
    }

    private static EventEnvelope Wrap(string eventType, string topic, object payload, string partitionKey)
        => new(
            Guid.NewGuid(),
            eventType,
            topic,
            payload,
            new PartitionKey(partitionKey),
            Timestamp.Now(),
            AggregateId: partitionKey,
            SequenceNumber: 0);
}
