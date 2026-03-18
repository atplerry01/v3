namespace Whycespace.CommandSystem.Tests;

using Whycespace.CommandSystem.Core.Dispatcher;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.Contracts.Runtime;

public class CommandDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ValidCommand_ReturnsExecutionResult()
    {
        var called = false;
        var dispatcher = new CommandDispatcher(async (cmd, ct) =>
        {
            called = true;
            return ExecutionResult.Ok(new Dictionary<string, object>
            {
                ["workflowName"] = "RideRequestWorkflow"
            });
        });

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["userId"] = "user-1" },
            DateTimeOffset.UtcNow);

        var result = await dispatcher.DispatchAsync(envelope);

        Assert.True(result.Success);
        Assert.True(called);
        Assert.Equal("RideRequestWorkflow", result.Output["workflowName"]);
    }

    [Fact]
    public async Task DispatchAsync_DuplicateCommand_PropagatesException()
    {
        var dispatcher = new CommandDispatcher((cmd, ct) =>
            throw new InvalidOperationException("Duplicate command"));

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(envelope));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public async Task DispatchAsync_DelegatesToRuntimeDispatcher()
    {
        CommandEnvelope? receivedCommand = null;
        var dispatcher = new CommandDispatcher(async (cmd, ct) =>
        {
            receivedCommand = cmd;
            return ExecutionResult.Ok();
        });

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "ListPropertyCommand",
            new Dictionary<string, object> { ["title"] = "Apt 1" },
            DateTimeOffset.UtcNow);

        await dispatcher.DispatchAsync(envelope);

        Assert.NotNull(receivedCommand);
        Assert.Equal(envelope.CommandId, receivedCommand!.CommandId);
        Assert.Equal("ListPropertyCommand", receivedCommand.CommandType);
    }

    [Fact]
    public async Task DispatchAsync_FailedResult_ReturnsFailed()
    {
        var dispatcher = new CommandDispatcher(async (cmd, ct) =>
            ExecutionResult.Fail("workflow not found"));

        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "UnknownCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var result = await dispatcher.DispatchAsync(envelope);

        Assert.False(result.Success);
        Assert.Contains("workflow not found", result.ErrorMessage);
    }
}
