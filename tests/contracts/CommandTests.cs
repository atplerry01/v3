namespace Whycespace.Contracts.Tests;

using Whycespace.Contracts.Commands;

public sealed class CommandTests
{
    [Fact]
    public void ICommand_Requires_CommandId_And_Timestamp()
    {
        var command = new TestCommand(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.NotEqual(Guid.Empty, command.CommandId);
        Assert.True(command.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CommandBase_Creates_Immutable_Command()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var command = new TestCommand(id, timestamp);

        Assert.Equal(id, command.CommandId);
        Assert.Equal(timestamp, command.Timestamp);
    }

    [Fact]
    public void CommandBase_Supports_Record_Equality()
    {
        var id = Guid.NewGuid();
        var ts = DateTimeOffset.UtcNow;
        var a = new TestCommand(id, ts);
        var b = new TestCommand(id, ts);

        Assert.Equal(a, b);
    }

    private sealed record TestCommand(Guid CommandId, DateTimeOffset Timestamp) : CommandBase(CommandId, Timestamp);
}
