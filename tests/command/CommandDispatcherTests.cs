namespace Whycespace.CommandSystem.Tests;

using Whycespace.CommandSystem.Dispatcher;
using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Routing;
using Whycespace.CommandSystem.Validation;

public class CommandDispatcherTests
{
    private readonly CommandValidator _validator = new();
    private readonly InMemoryIdempotencyRegistry _idempotency = new();
    private readonly CommandRouter _router = new();
    private readonly CommandDispatcher _dispatcher;

    public CommandDispatcherTests()
    {
        _router.MapCommand("RequestRideCommand", "RideRequestWorkflow");
        _router.MapCommand("ListPropertyCommand", "PropertyListingWorkflow");
        _router.MapCommand("AllocateCapitalCommand", "EconomicLifecycleWorkflow");
        _dispatcher = new CommandDispatcher(_validator, _idempotency, _router);
    }

    [Fact]
    public void Dispatch_ValidCommand_ReturnsWorkflowExecutionRequest()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["userId"] = "user-1" },
            DateTimeOffset.UtcNow);

        var result = _dispatcher.Dispatch(envelope);

        Assert.Equal("RideRequestWorkflow", result.WorkflowName);
        Assert.Equal(envelope.CommandId.ToString(), result.CorrelationId);
        Assert.Equal("user-1", result.Context["userId"]);
    }

    [Fact]
    public void Dispatch_DuplicateCommandId_Throws()
    {
        var id = Guid.NewGuid();
        var envelope = new CommandEnvelope(
            id, "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        _dispatcher.Dispatch(envelope);

        var ex = Assert.Throws<InvalidOperationException>(() => _dispatcher.Dispatch(envelope));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public void Dispatch_UnmappedCommandType_Throws()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "UnknownCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<InvalidOperationException>(() => _dispatcher.Dispatch(envelope));
        Assert.Contains("No workflow mapped", ex.Message);
    }

    [Fact]
    public void Dispatch_InvalidEnvelope_ThrowsValidation()
    {
        var envelope = new CommandEnvelope(
            Guid.Empty,
            "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => _dispatcher.Dispatch(envelope));
    }

    [Fact]
    public void Dispatch_PropertyListCommand_ResolvesCorrectWorkflow()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "ListPropertyCommand",
            new Dictionary<string, object> { ["title"] = "Apt 1" },
            DateTimeOffset.UtcNow);

        var result = _dispatcher.Dispatch(envelope);
        Assert.Equal("PropertyListingWorkflow", result.WorkflowName);
    }

    [Fact]
    public void Dispatch_AllocateCapitalCommand_ResolvesCorrectWorkflow()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "AllocateCapitalCommand",
            new Dictionary<string, object> { ["amount"] = 1000m },
            DateTimeOffset.UtcNow);

        var result = _dispatcher.Dispatch(envelope);
        Assert.Equal("EconomicLifecycleWorkflow", result.WorkflowName);
    }
}
