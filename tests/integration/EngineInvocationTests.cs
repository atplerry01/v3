namespace Whycespace.Tests.Integration;

using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Engines.T0U.WhycePolicy.Validation;
using Whycespace.Engines.T2E;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi.Engines;
using Whycespace.Engines.T2E.Clusters.Property.Letting.Engines;
using Whycespace.Engines.T4A.API;
using Whycespace.Engines.T4A.Interface.Auth;
using Whycespace.Engines.T4A.Tools.Developer;
using Whycespace.Engines.T4A.Interface.Integration;
using Whycespace.Runtime.Dispatcher;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Contracts.Engines;
using Xunit;

/// <summary>
/// Integration tests exercising RuntimeDispatcher → EngineRegistry → Engine → EngineResult.
/// Each test sends an EngineInvocationEnvelope through the dispatcher and verifies
/// the engine produces the expected events and output.
/// </summary>
public sealed class EngineInvocationTests
{
    private readonly RuntimeDispatcher _dispatcher;

    public EngineInvocationTests()
    {
        var registry = new EngineRegistry();

        // T0U Constitutional
        registry.Register(new PolicyValidationEngine());
        registry.Register(new ChainVerificationEngine());

        // T2E Execution
        registry.Register(new RideExecutionEngine());
        registry.Register(new PropertyExecutionEngine());

        // T3I Intelligence
        registry.Register(new DriverMatchingEngine());
        registry.Register(new TenantMatchingEngine());

        // T4A Access
        registry.Register(new Whycespace.Engines.T4A.Interface.Auth.AuthenticationEngine());
        registry.Register(new Whycespace.Engines.T4A.Interface.Auth.AuthorizationEngine());
        registry.Register(new APIEngine());
        registry.Register(new IntegrationEngine());
        registry.Register(new DeveloperToolsEngine());

        _dispatcher = new RuntimeDispatcher(registry);
    }

    private static EngineInvocationEnvelope Envelope(
        string engineName,
        string step,
        Dictionary<string, object> context) =>
        new(Guid.NewGuid(), engineName, Guid.NewGuid().ToString(), step, "default", context);

    // ---------------------------------------------------------------
    // T0U Constitutional tier
    // ---------------------------------------------------------------

    [Fact]
    public async Task PolicyValidation_DefaultPolicy_Succeeds()
    {
        var envelope = Envelope("PolicyValidation", "validate",
            new Dictionary<string, object> { ["policyType"] = "default" });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("PolicyValidated", result.Events[0].EventType);
    }

    [Fact]
    public async Task PolicyValidation_IdentityPolicy_RequiresUserId()
    {
        var envelope = Envelope("PolicyValidation", "validate",
            new Dictionary<string, object> { ["policyType"] = "identity" });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task PolicyValidation_IdentityPolicy_PassesWithUserId()
    {
        var envelope = Envelope("PolicyValidation", "validate",
            new Dictionary<string, object>
            {
                ["policyType"] = "identity",
                ["userId"] = Guid.NewGuid().ToString()
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
    }

    // ---------------------------------------------------------------
    // T2E Execution tier — RideExecution
    // ---------------------------------------------------------------

    [Fact]
    public async Task RideExecution_ValidateRequest_WithPickup_ProducesEvent()
    {
        var envelope = Envelope("RideExecution", "ValidateRequest",
            new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("RideRequestValidated", result.Events[0].EventType);
    }

    [Fact]
    public async Task RideExecution_ValidateRequest_MissingPickup_Fails()
    {
        var envelope = Envelope("RideExecution", "ValidateRequest",
            new Dictionary<string, object>());

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RideExecution_AssignDriver_ProducesDriverAssignedEvent()
    {
        var driverId = Guid.NewGuid().ToString();
        var envelope = Envelope("RideExecution", "AssignDriver",
            new Dictionary<string, object> { ["assignedDriverId"] = driverId });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("DriverAssigned", result.Events[0].EventType);
        Assert.Equal(driverId, result.Events[0].Payload["driverId"]);
    }

    [Fact]
    public async Task RideExecution_CompleteTrip_ProducesTripCompletedEvent()
    {
        var envelope = Envelope("RideExecution", "CompleteTrip",
            new Dictionary<string, object> { ["fare"] = 25.50m });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("TripCompleted", result.Events[0].EventType);
    }

    // ---------------------------------------------------------------
    // T2E Execution tier — PropertyExecution
    // ---------------------------------------------------------------

    [Fact]
    public async Task PropertyExecution_ValidateListing_WithDetails_ProducesEvent()
    {
        var envelope = Envelope("PropertyExecution", "ValidateListing",
            new Dictionary<string, object>
            {
                ["title"] = "Flat",
                ["monthlyRent"] = 1500m
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("ListingValidated", result.Events[0].EventType);
    }

    [Fact]
    public async Task PropertyExecution_PublishListing_ProducesListingPublishedEvent()
    {
        var envelope = Envelope("PropertyExecution", "PublishListing",
            new Dictionary<string, object>
            {
                ["title"] = "Studio",
                ["monthlyRent"] = 900m
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("ListingPublished", result.Events[0].EventType);
    }

    // ---------------------------------------------------------------
    // T2E Execution tier — EconomicExecution
    // ---------------------------------------------------------------

    [Fact]
    public async Task EconomicExecution_AllocateCapital_ProducesCapitalAllocatedEvent()
    {
        var envelope = Envelope("EconomicExecution", "AllocateCapital",
            new Dictionary<string, object> { ["amount"] = 50000m });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("CapitalAllocated", result.Events[0].EventType);
    }

    [Fact]
    public async Task EconomicExecution_CreateSpv_ProducesSpvCreatedEvent()
    {
        var envelope = Envelope("EconomicExecution", "CreateSpv",
            new Dictionary<string, object> { ["spvName"] = "InvestmentCo" });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("SpvCreated", result.Events[0].EventType);
    }

    [Fact]
    public async Task EconomicExecution_FullCycle_AllStepsSucceed()
    {
        var steps = new[]
        {
            ("AllocateCapital", new Dictionary<string, object> { ["amount"] = 100000m }),
            ("CreateSpv", new Dictionary<string, object> { ["spvName"] = "CycleCo", ["amount"] = 100000m }),
            ("RecordRevenue", new Dictionary<string, object> { ["spvName"] = "CycleCo" }),
            ("DistributeProfit", new Dictionary<string, object> { ["spvName"] = "CycleCo" })
        };

        foreach (var (step, ctx) in steps)
        {
            var envelope = Envelope("EconomicExecution", step, ctx);
            var result = await _dispatcher.DispatchAsync(envelope);
            Assert.True(result.Success, $"Step '{step}' failed: {result.Output.GetValueOrDefault("error")}");
        }
    }

    // ---------------------------------------------------------------
    // T3I Intelligence tier
    // ---------------------------------------------------------------

    [Fact]
    public async Task DriverMatching_WithCoordinates_ProducesDriverMatchedEvent()
    {
        var envelope = Envelope("DriverMatching", "match",
            new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("DriverMatched", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("assignedDriverId"));
    }

    // ---------------------------------------------------------------
    // T4A Access tier — APIEngine
    // ---------------------------------------------------------------

    [Fact]
    public async Task APIEngine_ValidRideRequest_ProducesAPICommandAcceptedEvent()
    {
        var envelope = Envelope("API", "api",
            new Dictionary<string, object>
            {
                ["apiAction"] = "ride.request",
                ["userId"] = Guid.NewGuid().ToString(),
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("APICommandAccepted", result.Events[0].EventType);
        Assert.Equal("RequestRide", result.Output["commandType"]);
        Assert.True((bool)result.Output["accepted"]);
    }

    [Fact]
    public async Task APIEngine_MissingUserId_Fails()
    {
        var envelope = Envelope("API", "api",
            new Dictionary<string, object> { ["apiAction"] = "ride.request" });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
    }

    // ---------------------------------------------------------------
    // T4A Access tier — IntegrationEngine
    // ---------------------------------------------------------------

    [Fact]
    public async Task IntegrationEngine_Office365_ProducesEvent()
    {
        var envelope = Envelope("Integration", "integration",
            new Dictionary<string, object>
            {
                ["provider"] = "office365",
                ["integrationAction"] = "email.send",
                ["target"] = "user@example.com",
                ["subject"] = "Test"
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("IntegrationOffice365Requested", result.Events[0].EventType);
        Assert.True((bool)result.Output["dispatched"]);
    }

    [Fact]
    public async Task IntegrationEngine_UnknownProvider_Fails()
    {
        var envelope = Envelope("Integration", "integration",
            new Dictionary<string, object>
            {
                ["provider"] = "nonexistent",
                ["integrationAction"] = "some.action"
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
    }

    // ---------------------------------------------------------------
    // T4A Access tier — DeveloperToolsEngine
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeveloperTools_ContextDump_ReturnsContextSummary()
    {
        var envelope = Envelope("DeveloperTools", "devtool",
            new Dictionary<string, object>
            {
                ["tool"] = "context.dump",
                ["environment"] = "development",
                ["someKey"] = "someValue"
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.Equal("DevContextDumped", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("dataKeyCount"));
    }

    [Fact]
    public async Task DeveloperTools_BlockedInProduction()
    {
        var envelope = Envelope("DeveloperTools", "devtool",
            new Dictionary<string, object>
            {
                ["tool"] = "context.dump",
                ["environment"] = "production"
            });

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
    }

    // ---------------------------------------------------------------
    // Dispatcher: unknown engine
    // ---------------------------------------------------------------

    [Fact]
    public async Task Dispatcher_UnknownEngine_Fails()
    {
        var envelope = Envelope("NonExistentEngine", "step",
            new Dictionary<string, object>());

        var result = await _dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
        Assert.Contains("NonExistentEngine", result.Output["error"] as string);
    }
}
