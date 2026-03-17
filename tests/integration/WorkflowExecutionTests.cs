namespace Whycespace.Tests.Integration;

using Whycespace.Application.Commands;
using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T2E;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Engines.T2E.Clusters.Property.Letting;
using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;
using Whycespace.Runtime.Dispatcher;
using Whycespace.EngineRuntime.Registry;
using Whycespace.WorkflowRuntime;
using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Dispatcher;
using Whycespace.Systems.Midstream.WSS.Mapping;
using Whycespace.Systems.Midstream.WSS.Orchestration;
using Whycespace.Systems.Midstream.WSS.Routing;
using Whycespace.Systems.Midstream.WSS.Workflows;
using Xunit;

/// <summary>
/// Integration tests exercising the full pipeline:
/// Command → WorkflowRouter → WSSOrchestrator → WorkflowOrchestrator
///         → RuntimeDispatcher → Engine → EngineResult → WorkflowState.
/// </summary>
public sealed class WorkflowExecutionTests
{
    private readonly EngineRegistry _registry;
    private readonly WorkflowStateStore _stateStore;
    private readonly CommandDispatcher _commandDispatcher;

    public WorkflowExecutionTests()
    {
        _registry = new EngineRegistry();
        _registry.Register(new PolicyValidationEngine());
        _registry.Register(new DriverMatchingEngine());
        _registry.Register(new RideExecutionEngine());
        _registry.Register(new PropertyExecutionEngine());

        var dispatcher = new RuntimeDispatcher(_registry);
        _stateStore = new WorkflowStateStore();
        var orchestrator = new WorkflowOrchestrator(dispatcher, _stateStore);

        var mapper = new WorkflowMapper();
        mapper.Register(new RideRequestWorkflow());
        mapper.Register(new PropertyListingWorkflow());
        mapper.Register(new EconomicLifecycleWorkflow());

        var wss = new WSSOrchestrator(mapper, orchestrator);
        var router = new WorkflowRouter();
        router.MapCommand<RequestRideCommand>("RideRequest");
        router.MapCommand<ListPropertyCommand>("PropertyListing");
        router.MapCommand<AllocateCapitalCommand>("EconomicLifecycle");

        _commandDispatcher = new CommandDispatcher(router, wss);
    }

    // ---------------------------------------------------------------
    // Ride Request — full pipeline
    // ---------------------------------------------------------------

    [Fact]
    public async Task RideRequest_FullPipeline_CompletesWorkflow()
    {
        var command = new RequestRideCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            new Shared.Location.GeoLocation(51.5074, -0.1278),
            new Shared.Location.GeoLocation(51.5155, -0.1419));

        var context = new Dictionary<string, object>
        {
            ["userId"] = command.UserId.ToString(),
            ["pickupLatitude"] = 51.5074,
            ["pickupLongitude"] = -0.1278,
            ["dropoffLatitude"] = 51.5155,
            ["dropoffLongitude"] = -0.1419
        };

        var state = await _commandDispatcher.DispatchAsync(command, context);

        // Workflow executes through PolicyValidation → DriverMatching
        // → RideExecution (ValidateRequest, AssignDriver, CompleteTrip).
        // Step IDs are now aligned with engine expectations.
        Assert.NotNull(state);
        Assert.Equal(WorkflowStatus.Completed, state.Status);
        Assert.NotNull(state.CompletedAt);

        // Verify workflow is persisted in state store
        var persisted = _stateStore.GetAll();
        Assert.Single(persisted);
    }

    // ---------------------------------------------------------------
    // Ride Request — custom graph with matching step IDs
    // ---------------------------------------------------------------

    [Fact]
    public async Task RideRequest_CustomGraph_FullExecution()
    {
        // Build a graph with step IDs that match what the engines expect,
        // testing the full dispatcher → engine chain end-to-end.
        var graph = new WorkflowGraph(Guid.NewGuid().ToString(), "RideRequestCustom", new List<WorkflowStep>
        {
            new("validate-policy", "Validate Policy", "PolicyValidation", new[] { "match-driver" }),
            new("match-driver", "Match Driver", "DriverMatching", new[] { "ValidateRequest" }),
            new("ValidateRequest", "Validate Request", "RideExecution", new[] { "AssignDriver" }),
            new("AssignDriver", "Assign Driver", "RideExecution", Array.Empty<string>())
        });

        var context = new Dictionary<string, object>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["pickupLatitude"] = 51.5074,
            ["pickupLongitude"] = -0.1278
        };

        var dispatcher = new RuntimeDispatcher(_registry);
        var stateStore = new WorkflowStateStore();
        var orchestrator = new WorkflowOrchestrator(dispatcher, stateStore);

        var state = await orchestrator.ExecuteWorkflowAsync(graph, context);

        // PolicyValidation passes (default policy allows all)
        // DriverMatching passes (coordinates present), outputs assignedDriverId
        // RideExecution/ValidateRequest passes (pickupLatitude present)
        // RideExecution/AssignDriver passes (assignedDriverId was set by DriverMatching)
        Assert.Equal(WorkflowStatus.Completed, state.Status);
        Assert.NotNull(state.CompletedAt);
        Assert.True(state.Context.ContainsKey("assignedDriverId"));
    }

    // ---------------------------------------------------------------
    // Property Listing — full pipeline
    // ---------------------------------------------------------------

    [Fact]
    public async Task PropertyListing_FullPipeline_ExecutesWorkflow()
    {
        var command = new ListPropertyCommand(
            Guid.NewGuid(), Guid.NewGuid(), "2 Bed Flat", "Nice flat",
            new Shared.Location.GeoLocation(51.5074, -0.1278), 1500m);

        var context = new Dictionary<string, object>
        {
            ["userId"] = command.OwnerId.ToString(),
            ["title"] = "2 Bed Flat",
            ["description"] = "Nice flat",
            ["monthlyRent"] = 1500m
        };

        var state = await _commandDispatcher.DispatchAsync(command, context);

        Assert.NotNull(state);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public async Task PropertyListing_CustomGraph_FullExecution()
    {
        var graph = new WorkflowGraph(Guid.NewGuid().ToString(), "PropertyListingCustom", new List<WorkflowStep>
        {
            new("ValidateListing", "Validate Listing", "PropertyExecution", new[] { "PublishListing" }),
            new("PublishListing", "Publish Listing", "PropertyExecution", Array.Empty<string>())
        });

        var context = new Dictionary<string, object>
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["title"] = "Studio Apartment",
            ["monthlyRent"] = 900m
        };

        var dispatcher = new RuntimeDispatcher(_registry);
        var stateStore = new WorkflowStateStore();
        var orchestrator = new WorkflowOrchestrator(dispatcher, stateStore);

        var state = await orchestrator.ExecuteWorkflowAsync(graph, context);

        Assert.Equal(WorkflowStatus.Completed, state.Status);
    }

    // ---------------------------------------------------------------
    // Economic Lifecycle — full pipeline
    // ---------------------------------------------------------------

    [Fact]
    public async Task EconomicLifecycle_FullPipeline_ExecutesWorkflow()
    {
        var command = new AllocateCapitalCommand(Guid.NewGuid(), Guid.NewGuid(), 50000m, "Investment");

        var context = new Dictionary<string, object>
        {
            ["amount"] = 50000m,
            ["currency"] = "GBP",
            ["spvName"] = "TestSPV"
        };

        var state = await _commandDispatcher.DispatchAsync(command, context);

        Assert.NotNull(state);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public async Task EconomicLifecycle_CustomGraph_FullExecution()
    {
        var graph = new WorkflowGraph(Guid.NewGuid().ToString(), "EconomicLifecycleCustom", new List<WorkflowStep>
        {
            new("AllocateCapital", "Allocate Capital", "EconomicExecution", new[] { "CreateSpv" }),
            new("CreateSpv", "Create SPV", "EconomicExecution", new[] { "RecordRevenue" }),
            new("RecordRevenue", "Record Revenue", "EconomicExecution", new[] { "DistributeProfit" }),
            new("DistributeProfit", "Distribute Profit", "EconomicExecution", Array.Empty<string>())
        });

        var context = new Dictionary<string, object>
        {
            ["amount"] = 50000m,
            ["spvName"] = "InvestmentVehicle",
            ["currency"] = "GBP"
        };

        var dispatcher = new RuntimeDispatcher(_registry);
        var stateStore = new WorkflowStateStore();
        var orchestrator = new WorkflowOrchestrator(dispatcher, stateStore);

        var state = await orchestrator.ExecuteWorkflowAsync(graph, context);

        Assert.Equal(WorkflowStatus.Completed, state.Status);
    }

    // ---------------------------------------------------------------
    // Cross-cutting
    // ---------------------------------------------------------------

    [Fact]
    public async Task WorkflowStateStore_TracksMultipleConcurrentWorkflows()
    {
        var ride = new RequestRideCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            new Shared.Location.GeoLocation(51.5, -0.1),
            new Shared.Location.GeoLocation(51.6, -0.2));

        var property = new ListPropertyCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Flat", "Nice", new Shared.Location.GeoLocation(51.5, -0.1), 1200m);

        var rideCtx = new Dictionary<string, object>
        {
            ["userId"] = ride.UserId.ToString(),
            ["pickupLatitude"] = 51.5,
            ["pickupLongitude"] = -0.1
        };

        var propCtx = new Dictionary<string, object>
        {
            ["userId"] = property.OwnerId.ToString(),
            ["title"] = "Flat",
            ["monthlyRent"] = 1200m
        };

        var rideTask = _commandDispatcher.DispatchAsync(ride, rideCtx);
        var propTask = _commandDispatcher.DispatchAsync(property, propCtx);

        await Task.WhenAll(rideTask, propTask);

        var allStates = _stateStore.GetAll();
        Assert.Equal(2, allStates.Count);
    }
}
