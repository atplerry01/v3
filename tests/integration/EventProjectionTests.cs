namespace Whycespace.Tests.Integration;

using Whycespace.Engines.T2E;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Engines.T2E.Clusters.Property.Letting;
using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Events;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Workflows;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Consumers;
using Whycespace.Projections.Engine;
using Whycespace.Projections.Projections;
using Whycespace.Projections.Registry;
using Whycespace.Projections.Storage;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;
using Xunit;

/// <summary>
/// Integration tests verifying the full event → projection pipeline using the CQRS projection system.
/// </summary>
public sealed class EventProjectionTests
{
    private readonly EngineRegistry _registry;

    public EventProjectionTests()
    {
        _registry = new EngineRegistry();
        _registry.Register(new RideExecutionEngine());
        _registry.Register(new PropertyExecutionEngine());
        _registry.Register(new EconomicExecutionEngine());
        _registry.Register(new DriverMatchingEngine());
    }

    private static EngineInvocationEnvelope Envelope(
        string engineName,
        string step,
        Dictionary<string, object> context) =>
        new(Guid.NewGuid(), engineName, Guid.NewGuid().ToString(), step, "default", context);

    private static EventEnvelope ToEnvelope(SystemEvent @event) =>
        new(@event.EventId,
            @event.EventType,
            "whyce.events",
            @event.Payload,
            new PartitionKey(@event.AggregateId.ToString()),
            new Timestamp(@event.Timestamp));

    private static async Task PublishEngineEventsAsync(
        EngineResult result,
        ProjectionEventConsumer consumer)
    {
        foreach (var engineEvent in result.Events)
        {
            var systemEvent = SystemEvent.Create(
                engineEvent.EventType,
                engineEvent.AggregateId,
                new Dictionary<string, object>(engineEvent.Payload));
            await consumer.ConsumeAsync(ToEnvelope(systemEvent));
        }
    }

    private static (RedisProjectionStore store, ProjectionEventConsumer consumer) SetupProjectionSystem(
        params IProjection[] projections)
    {
        var store = new RedisProjectionStore();
        var registry = new ProjectionRegistry();
        foreach (var p in projections)
            registry.Register(p);

        var engine = new ProjectionEngine(registry);
        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());
        var consumer = new ProjectionEventConsumer(engine, guard);
        return (store, consumer);
    }

    // ---------------------------------------------------------------
    // DriverLocationProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task DriverLocationProjection_UpdatesOnDriverLocationUpdatedEvent()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var driverId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["driverId"] = driverId,
                ["latitude"] = 51.5074,
                ["longitude"] = -0.1278
            });

        await consumer.ConsumeAsync(ToEnvelope(evt));

        var result = await store.GetAsync($"driver:{driverId}");
        Assert.NotNull(result);
        Assert.Contains("51.5074", result);
    }

    [Fact]
    public async Task DriverLocationProjection_TracksMultipleDrivers()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        for (var i = 0; i < 5; i++)
        {
            var evt = SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
                new Dictionary<string, object>
                {
                    ["driverId"] = $"driver-{i}",
                    ["latitude"] = 51.5 + i * 0.01,
                    ["longitude"] = -0.1 + i * 0.01
                });
            await consumer.ConsumeAsync(ToEnvelope(evt));
        }

        for (var i = 0; i < 5; i++)
        {
            var result = await store.GetAsync($"driver:driver-{i}");
            Assert.NotNull(result);
        }
    }

    [Fact]
    public async Task DriverLocationProjection_UpdatesExistingDriverPosition()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var driverId = "driver-1";

        await consumer.ConsumeAsync(ToEnvelope(SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 51.0, ["longitude"] = -0.1 })));

        await consumer.ConsumeAsync(ToEnvelope(SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 52.0, ["longitude"] = -0.2 })));

        var result = await store.GetAsync($"driver:{driverId}");
        Assert.NotNull(result);
        Assert.Contains("52", result);
    }

    // ---------------------------------------------------------------
    // PropertyListingProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task PropertyListingProjection_UpdatesOnEvent()
    {
        var store = new RedisProjectionStore();
        var projection = new PropertyListingProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var propertyId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("PropertyListingCreatedEvent", Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["propertyId"] = propertyId,
                ["address"] = "123 Main St"
            });

        await consumer.ConsumeAsync(ToEnvelope(evt));

        var result = await store.GetAsync($"property:{propertyId}");
        Assert.NotNull(result);
        Assert.Contains("123 Main St", result);
    }

    // ---------------------------------------------------------------
    // VaultBalanceProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task VaultBalanceProjection_CapitalContribution_IncreasesBalance()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("CapitalContributionRecordedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 10000m });

        await consumer.ConsumeAsync(ToEnvelope(evt));

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("10000", result);
    }

    [Fact]
    public async Task VaultBalanceProjection_ProfitDistributed_IncreasesBalance()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("ProfitDistributedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 5000m });

        await consumer.ConsumeAsync(ToEnvelope(evt));

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("5000", result);
    }

    // ---------------------------------------------------------------
    // RevenueProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task RevenueProjection_UpdatesOnRevenueRecordedEvent()
    {
        var store = new RedisProjectionStore();
        var projection = new RevenueProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var aggregateId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("RevenueRecordedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["aggregateId"] = aggregateId, ["amount"] = 5000m });

        await consumer.ConsumeAsync(ToEnvelope(evt));

        var result = await store.GetAsync($"revenue:{aggregateId}");
        Assert.NotNull(result);
        Assert.Contains("5000", result);
    }

    // ---------------------------------------------------------------
    // Idempotency
    // ---------------------------------------------------------------

    [Fact]
    public async Task Projection_Idempotent_DuplicateEventsIgnored()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);
        var (_, consumer) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            "CapitalContributionRecordedEvent",
            "whyce.economic.events",
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 10000m },
            new PartitionKey(vaultId),
            Timestamp.Now());

        await consumer.ConsumeAsync(envelope);
        await consumer.ConsumeAsync(envelope);

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("10000", result);
    }

    // ---------------------------------------------------------------
    // EventBus isolation
    // ---------------------------------------------------------------

    [Fact]
    public async Task EventBus_GetPublishedEvents_TracksAll()
    {
        var eventBus = new EventBus();

        await eventBus.PublishAsync(SystemEvent.Create("A", Guid.NewGuid()));
        await eventBus.PublishAsync(SystemEvent.Create("B", Guid.NewGuid()));
        await eventBus.PublishAsync(SystemEvent.Create("C", Guid.NewGuid()));

        Assert.Equal(3, eventBus.GetPublishedEvents().Count);
    }
}
