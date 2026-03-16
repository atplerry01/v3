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
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Core.Economics;
using Whycespace.ProjectionRuntime.Projections.Clusters.Mobility;
using Whycespace.ProjectionRuntime.Projections.Clusters.Property;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRuntime.Storage;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;
using Xunit;

/// <summary>
/// Integration tests verifying the full event -> projection pipeline using the CQRS projection system.
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

    private static async Task ProcessWithGuardAsync(
        EventEnvelope envelope,
        IProjectionRegistry projectionRegistry,
        EventProcessingGuard guard)
    {
        if (!guard.ShouldProcess(envelope))
            return;

        var projections = projectionRegistry.Resolve(envelope.EventType);
        foreach (var p in projections)
            await p.HandleAsync(envelope);
    }

    private static async Task PublishEngineEventsAsync(
        EngineResult result,
        IProjectionRegistry projectionRegistry,
        EventProcessingGuard guard)
    {
        foreach (var engineEvent in result.Events)
        {
            var systemEvent = SystemEvent.Create(
                engineEvent.EventType,
                engineEvent.AggregateId,
                new Dictionary<string, object>(engineEvent.Payload));
            await ProcessWithGuardAsync(ToEnvelope(systemEvent), projectionRegistry, guard);
        }
    }

    private static (RedisProjectionStore store, ProjectionRegistry registry, EventProcessingGuard guard) SetupProjectionSystem(
        params IProjection[] projections)
    {
        var store = new RedisProjectionStore();
        var registry = new ProjectionRegistry();
        foreach (var p in projections)
            registry.Register(p);

        var guard = new EventProcessingGuard(new EventDeduplicationRegistry());
        return (store, registry, guard);
    }

    // ---------------------------------------------------------------
    // DriverLocationProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task DriverLocationProjection_UpdatesOnDriverLocationUpdatedEvent()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var driverId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["driverId"] = driverId,
                ["latitude"] = 51.5074,
                ["longitude"] = -0.1278
            });

        await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);

        var result = await store.GetAsync($"driver:{driverId}");
        Assert.NotNull(result);
        Assert.Contains("51.5074", result);
    }

    [Fact]
    public async Task DriverLocationProjection_TracksMultipleDrivers()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);
        var (_, registry, guard) = SetupProjectionSystem(projection);

        for (var i = 0; i < 5; i++)
        {
            var evt = SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
                new Dictionary<string, object>
                {
                    ["driverId"] = $"driver-{i}",
                    ["latitude"] = 51.5 + i * 0.01,
                    ["longitude"] = -0.1 + i * 0.01
                });
            await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);
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
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var driverId = "driver-1";

        await ProcessWithGuardAsync(ToEnvelope(SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 51.0, ["longitude"] = -0.1 })), registry, guard);

        await ProcessWithGuardAsync(ToEnvelope(SystemEvent.Create("DriverLocationUpdatedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 52.0, ["longitude"] = -0.2 })), registry, guard);

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
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var propertyId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("PropertyListingCreatedEvent", Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["propertyId"] = propertyId,
                ["address"] = "123 Main St"
            });

        await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);

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
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("CapitalContributionRecordedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 10000m });

        await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("10000", result);
    }

    [Fact]
    public async Task VaultBalanceProjection_ProfitDistributed_IncreasesBalance()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("ProfitDistributedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 5000m });

        await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);

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
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var aggregateId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("RevenueRecordedEvent", Guid.NewGuid(),
            new Dictionary<string, object> { ["aggregateId"] = aggregateId, ["amount"] = 5000m });

        await ProcessWithGuardAsync(ToEnvelope(evt), registry, guard);

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
        var (_, registry, guard) = SetupProjectionSystem(projection);

        var vaultId = Guid.NewGuid().ToString();
        var eventId = Guid.NewGuid();
        var envelope = new EventEnvelope(
            eventId,
            "CapitalContributionRecordedEvent",
            "whyce.economic.events",
            new Dictionary<string, object> { ["vaultId"] = vaultId, ["amount"] = 10000m },
            new PartitionKey(vaultId),
            Timestamp.Now());

        await ProcessWithGuardAsync(envelope, registry, guard);
        await ProcessWithGuardAsync(envelope, registry, guard);

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
