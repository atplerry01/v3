namespace Whycespace.Tests.Integration;

using Whycespace.Engines.T2E_Execution;
using Whycespace.Engines.T3I_Intelligence;
using Whycespace.Runtime.Dispatcher;
using Whycespace.Runtime.Events;
using Whycespace.Runtime.Projections;
using Whycespace.Runtime.Registry;
using Whycespace.Runtime.Workflow;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Events;
using Whycespace.Contracts.Workflows;
using Xunit;

/// <summary>
/// Integration tests verifying the full event → projection pipeline:
/// Engine produces EngineEvents → events are published to EventBus as SystemEvents
///     → Projections subscribe and update read models.
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

    /// <summary>
    /// Bridges EngineEvents to the EventBus as SystemEvents, simulating what
    /// the runtime would do after an engine invocation completes.
    /// </summary>
    private static async Task PublishEngineEventsAsync(EngineResult result, EventBus eventBus)
    {
        foreach (var engineEvent in result.Events)
        {
            var systemEvent = SystemEvent.Create(
                engineEvent.EventType,
                engineEvent.AggregateId,
                new Dictionary<string, object>(engineEvent.Payload));
            await eventBus.PublishAsync(systemEvent);
        }
    }

    // ---------------------------------------------------------------
    // DriverLocationProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task DriverLocationProjection_UpdatesOnDriverLocationUpdatedEvent()
    {
        var projection = new DriverLocationProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("DriverLocationUpdated", projection.HandleAsync);

        var driverId = Guid.NewGuid().ToString();
        var evt = SystemEvent.Create("DriverLocationUpdated", Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["driverId"] = driverId,
                ["latitude"] = 51.5074,
                ["longitude"] = -0.1278
            });

        await eventBus.PublishAsync(evt);

        var locations = projection.GetLocations();
        Assert.True(locations.ContainsKey(driverId));
        Assert.Equal(51.5074, locations[driverId].Lat);
        Assert.Equal(-0.1278, locations[driverId].Lon);
    }

    [Fact]
    public async Task DriverLocationProjection_TracksMultipleDrivers()
    {
        var projection = new DriverLocationProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("DriverLocationUpdated", projection.HandleAsync);

        for (var i = 0; i < 5; i++)
        {
            var evt = SystemEvent.Create("DriverLocationUpdated", Guid.NewGuid(),
                new Dictionary<string, object>
                {
                    ["driverId"] = $"driver-{i}",
                    ["latitude"] = 51.5 + i * 0.01,
                    ["longitude"] = -0.1 + i * 0.01
                });
            await eventBus.PublishAsync(evt);
        }

        Assert.Equal(5, projection.GetLocations().Count);
    }

    [Fact]
    public async Task DriverLocationProjection_UpdatesExistingDriverPosition()
    {
        var projection = new DriverLocationProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("DriverLocationUpdated", projection.HandleAsync);

        var driverId = "driver-1";

        await eventBus.PublishAsync(SystemEvent.Create("DriverLocationUpdated", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 51.0, ["longitude"] = -0.1 }));

        await eventBus.PublishAsync(SystemEvent.Create("DriverLocationUpdated", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = driverId, ["latitude"] = 52.0, ["longitude"] = -0.2 }));

        var locations = projection.GetLocations();
        Assert.Single(locations);
        Assert.Equal(52.0, locations[driverId].Lat);
    }

    // ---------------------------------------------------------------
    // PropertyListingProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task PropertyListingProjection_UpdatesOnListingPublishedEvent()
    {
        var projection = new PropertyListingProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("ListingPublished", projection.HandleAsync);

        // Simulate engine → event → projection
        var dispatcher = new RuntimeDispatcher(_registry);
        var envelope = Envelope("PropertyExecution", "PublishListing",
            new Dictionary<string, object>
            {
                ["title"] = "3 Bed House",
                ["monthlyRent"] = 2000m
            });

        var result = await dispatcher.DispatchAsync(envelope);
        Assert.True(result.Success);

        await PublishEngineEventsAsync(result, eventBus);

        var listings = projection.GetListings();
        Assert.Single(listings);

        var listing = listings.Values.First();
        Assert.Equal("3 Bed House", listing["title"]);
    }

    [Fact]
    public async Task PropertyListingProjection_MultipleListings()
    {
        var projection = new PropertyListingProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("ListingPublished", projection.HandleAsync);

        var titles = new[] { "Studio", "1 Bed Flat", "Penthouse" };
        foreach (var title in titles)
        {
            var evt = SystemEvent.Create("ListingPublished", Guid.NewGuid(),
                new Dictionary<string, object> { ["title"] = title, ["monthlyRent"] = 1200m });
            await eventBus.PublishAsync(evt);
        }

        Assert.Equal(3, projection.GetListings().Count);
    }

    // ---------------------------------------------------------------
    // VaultBalanceProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task VaultBalanceProjection_CapitalAllocated_DecreasesBalance()
    {
        var projection = new VaultBalanceProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("CapitalAllocated", projection.HandleAsync);

        var vaultId = Guid.NewGuid();
        var evt = SystemEvent.Create("CapitalAllocated", vaultId,
            new Dictionary<string, object> { ["amount"] = 10000m });

        await eventBus.PublishAsync(evt);

        var balances = projection.GetBalances();
        Assert.Equal(-10000m, balances[vaultId.ToString()]);
    }

    [Fact]
    public async Task VaultBalanceProjection_ProfitDistributed_IncreasesBalance()
    {
        var projection = new VaultBalanceProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("ProfitDistributed", projection.HandleAsync);

        var vaultId = Guid.NewGuid();
        var evt = SystemEvent.Create("ProfitDistributed", vaultId,
            new Dictionary<string, object> { ["amount"] = 5000m });

        await eventBus.PublishAsync(evt);

        var balances = projection.GetBalances();
        Assert.Equal(5000m, balances[vaultId.ToString()]);
    }

    [Fact]
    public async Task VaultBalanceProjection_FullCycle_AllocateThenDistribute()
    {
        var projection = new VaultBalanceProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("CapitalAllocated", projection.HandleAsync);
        eventBus.Subscribe("ProfitDistributed", projection.HandleAsync);

        var vaultId = Guid.NewGuid();

        // Allocate 50,000
        await eventBus.PublishAsync(SystemEvent.Create("CapitalAllocated", vaultId,
            new Dictionary<string, object> { ["amount"] = 50000m }));

        // Distribute 20,000 profit
        await eventBus.PublishAsync(SystemEvent.Create("ProfitDistributed", vaultId,
            new Dictionary<string, object> { ["amount"] = 20000m }));

        // Net: -50000 + 20000 = -30000
        Assert.Equal(-30000m, projection.GetBalances()[vaultId.ToString()]);
    }

    [Fact]
    public async Task VaultBalanceProjection_EngineToProjection_EconomicExecution()
    {
        var projection = new VaultBalanceProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("CapitalAllocated", projection.HandleAsync);
        eventBus.Subscribe("ProfitDistributed", projection.HandleAsync);

        var dispatcher = new RuntimeDispatcher(_registry);

        // Engine: AllocateCapital → CapitalAllocated event
        var allocEnvelope = Envelope("EconomicExecution", "AllocateCapital",
            new Dictionary<string, object> { ["amount"] = 75000m });
        var allocResult = await dispatcher.DispatchAsync(allocEnvelope);
        Assert.True(allocResult.Success);
        await PublishEngineEventsAsync(allocResult, eventBus);

        // Engine: DistributeProfit → ProfitDistributed event (must include amount for projection)
        var distEnvelope = Envelope("EconomicExecution", "DistributeProfit",
            new Dictionary<string, object> { ["amount"] = 25000m });
        var distResult = await dispatcher.DispatchAsync(distEnvelope);
        Assert.True(distResult.Success);
        await PublishEngineEventsAsync(distResult, eventBus);

        // Two different aggregate IDs (workflow IDs), so two vault entries
        Assert.Equal(2, projection.GetBalances().Count);
    }

    // ---------------------------------------------------------------
    // RevenueProjection
    // ---------------------------------------------------------------

    [Fact]
    public async Task RevenueProjection_UpdatesOnRevenueRecordedEvent()
    {
        var projection = new RevenueProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("RevenueRecorded", projection.HandleAsync);

        var spvId = Guid.NewGuid();
        await eventBus.PublishAsync(SystemEvent.Create("RevenueRecorded", spvId,
            new Dictionary<string, object> { ["amount"] = 5000m, ["source"] = "Fare" }));

        Assert.Equal(5000m, projection.GetRevenues()[spvId.ToString()]);
    }

    [Fact]
    public async Task RevenueProjection_AccumulatesMultipleRecords()
    {
        var projection = new RevenueProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("RevenueRecorded", projection.HandleAsync);

        var spvId = Guid.NewGuid();
        await eventBus.PublishAsync(SystemEvent.Create("RevenueRecorded", spvId,
            new Dictionary<string, object> { ["amount"] = 3000m }));
        await eventBus.PublishAsync(SystemEvent.Create("RevenueRecorded", spvId,
            new Dictionary<string, object> { ["amount"] = 7000m }));

        Assert.Equal(10000m, projection.GetRevenues()[spvId.ToString()]);
    }

    [Fact]
    public async Task RevenueProjection_EngineToProjection_EconomicExecution()
    {
        var projection = new RevenueProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("RevenueRecorded", projection.HandleAsync);

        var dispatcher = new RuntimeDispatcher(_registry);

        var envelope = Envelope("EconomicExecution", "RecordRevenue",
            new Dictionary<string, object> { ["amount"] = 12000m, ["source"] = "Rental" });

        var result = await dispatcher.DispatchAsync(envelope);
        Assert.True(result.Success);

        await PublishEngineEventsAsync(result, eventBus);

        var revenues = projection.GetRevenues();
        Assert.Single(revenues);
    }

    // ---------------------------------------------------------------
    // Full end-to-end: Workflow → Engine → Event → Projection
    // ---------------------------------------------------------------

    [Fact]
    public async Task FullPipeline_EconomicWorkflow_UpdatesVaultAndRevenueProjections()
    {
        var vaultProjection = new VaultBalanceProjection();
        var revenueProjection = new RevenueProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("CapitalAllocated", vaultProjection.HandleAsync);
        eventBus.Subscribe("ProfitDistributed", vaultProjection.HandleAsync);
        eventBus.Subscribe("RevenueRecorded", revenueProjection.HandleAsync);

        var dispatcher = new RuntimeDispatcher(_registry);
        var stateStore = new WorkflowStateStore();
        var orchestrator = new WorkflowOrchestrator(dispatcher, stateStore);

        // Build workflow with step IDs matching engine expectations
        var graph = new WorkflowGraph(Guid.NewGuid().ToString(), "EconomicE2E", new List<WorkflowStep>
        {
            new("AllocateCapital", "Allocate", "EconomicExecution", new[] { "CreateSpv" }),
            new("CreateSpv", "Create SPV", "EconomicExecution", new[] { "RecordRevenue" }),
            new("RecordRevenue", "Record Revenue", "EconomicExecution", new[] { "DistributeProfit" }),
            new("DistributeProfit", "Distribute", "EconomicExecution", Array.Empty<string>())
        });

        var context = new Dictionary<string, object>
        {
            ["amount"] = 100000m,
            ["spvName"] = "E2E-SPV",
            ["currency"] = "GBP"
        };

        var state = await orchestrator.ExecuteWorkflowAsync(graph, context);
        Assert.Equal(WorkflowStatus.Completed, state.Status);

        // Now publish the events that each engine step would have produced.
        // In a real system, the runtime would bridge EngineEvents to the EventBus
        // after each step. Here we replay the steps to collect events.
        var steps = new[] { "AllocateCapital", "CreateSpv", "RecordRevenue", "DistributeProfit" };
        foreach (var step in steps)
        {
            var envelope = Envelope("EconomicExecution", step, new Dictionary<string, object>(context));
            var result = await dispatcher.DispatchAsync(envelope);
            if (result.Success)
                await PublishEngineEventsAsync(result, eventBus);
        }

        // Projections should have been updated
        Assert.NotEmpty(vaultProjection.GetBalances());
        Assert.NotEmpty(revenueProjection.GetRevenues());
    }

    [Fact]
    public async Task FullPipeline_PropertyWorkflow_UpdatesListingProjection()
    {
        var listingProjection = new PropertyListingProjection();
        var eventBus = new EventBus();
        eventBus.Subscribe("ListingPublished", listingProjection.HandleAsync);

        var dispatcher = new RuntimeDispatcher(_registry);

        // Simulate: PropertyExecution/PublishListing → ListingPublished → PropertyListingProjection
        var envelope = Envelope("PropertyExecution", "PublishListing",
            new Dictionary<string, object>
            {
                ["title"] = "Luxury Penthouse",
                ["monthlyRent"] = 5000m,
                ["description"] = "Top floor views"
            });

        var result = await dispatcher.DispatchAsync(envelope);
        Assert.True(result.Success);

        await PublishEngineEventsAsync(result, eventBus);

        var listings = listingProjection.GetListings();
        Assert.Single(listings);
        Assert.Equal("Luxury Penthouse", listings.Values.First()["title"]);
    }

    // ---------------------------------------------------------------
    // EventBus isolation
    // ---------------------------------------------------------------

    [Fact]
    public async Task EventBus_OnlyMatchingSubscribersReceiveEvents()
    {
        var driverProjection = new DriverLocationProjection();
        var listingProjection = new PropertyListingProjection();
        var eventBus = new EventBus();

        eventBus.Subscribe("DriverLocationUpdated", driverProjection.HandleAsync);
        eventBus.Subscribe("ListingPublished", listingProjection.HandleAsync);

        // Publish a driver event — listing projection should not update
        await eventBus.PublishAsync(SystemEvent.Create("DriverLocationUpdated", Guid.NewGuid(),
            new Dictionary<string, object> { ["driverId"] = "d1", ["latitude"] = 51.5, ["longitude"] = -0.1 }));

        Assert.Single(driverProjection.GetLocations());
        Assert.Empty(listingProjection.GetListings());

        // Publish a listing event — driver projection should not update
        await eventBus.PublishAsync(SystemEvent.Create("ListingPublished", Guid.NewGuid(),
            new Dictionary<string, object> { ["title"] = "Flat" }));

        Assert.Single(driverProjection.GetLocations());
        Assert.Single(listingProjection.GetListings());
    }

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
