namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T0U_Constitutional;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class PolicyValidationEngineTests
{
    private readonly PolicyValidationEngine _engine = new();

    [Fact]
    public async Task Name_ReturnsPolicyValidation()
    {
        Assert.Equal("PolicyValidation", _engine.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithIdentityPolicy_ValidatesUserId()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "validate",
            "partition-1", new Dictionary<string, object>
            {
                ["policyType"] = "identity",
                ["userId"] = "user-1"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("PolicyValidated", result.Events[0].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_WithIdentityPolicy_FailsWithoutUserId()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "validate",
            "partition-1", new Dictionary<string, object>
            {
                ["policyType"] = "identity"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
