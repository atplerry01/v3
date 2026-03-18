namespace Whycespace.CommandSystem.Tests;

using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Validation;

public class CommandValidatorTests
{
    private readonly CommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_DoesNotThrow()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "TestCommand",
            new Dictionary<string, object> { ["key"] = "value" },
            DateTimeOffset.UtcNow);

        _validator.Validate(envelope);
    }

    [Fact]
    public void Validate_EmptyCommandId_Throws()
    {
        var envelope = new CommandEnvelope(
            Guid.Empty,
            "TestCommand",
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<InvalidOperationException>(() => _validator.Validate(envelope));
        Assert.Contains("CommandId", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrEmptyCommandType_Throws(string? commandType)
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            commandType!,
            new Dictionary<string, object>(),
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<InvalidOperationException>(() => _validator.Validate(envelope));
        Assert.Contains("CommandType", ex.Message);
    }

    [Fact]
    public void Validate_NullPayload_Throws()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "TestCommand",
            null!,
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<InvalidOperationException>(() => _validator.Validate(envelope));
        Assert.Contains("Payload", ex.Message);
    }
}
