using Whycespace.ReliabilityRuntime.Retry;

namespace Whycespace.ReliabilityRuntime.Tests;

public sealed class RetryPolicyManagerTests
{
    [Fact]
    public void ShouldRetry_ReturnsTrue_WhenUnderMax()
    {
        var manager = new RetryPolicyManager(3);

        Assert.True(manager.ShouldRetry("exec-1"));
        Assert.True(manager.ShouldRetry("exec-1"));
        Assert.True(manager.ShouldRetry("exec-1"));
    }

    [Fact]
    public void ShouldRetry_ReturnsFalse_WhenExceedsMax()
    {
        var manager = new RetryPolicyManager(2);

        manager.ShouldRetry("exec-1");
        manager.ShouldRetry("exec-1");

        Assert.False(manager.ShouldRetry("exec-1"));
    }

    [Fact]
    public void GetAttempts_ReturnsZero_WhenNoAttempts()
    {
        var manager = new RetryPolicyManager(3);

        Assert.Equal(0, manager.GetAttempts("exec-1"));
    }

    [Fact]
    public void GetAttempts_ReturnsCorrectCount()
    {
        var manager = new RetryPolicyManager(3);

        manager.ShouldRetry("exec-1");
        manager.ShouldRetry("exec-1");

        Assert.Equal(2, manager.GetAttempts("exec-1"));
    }
}
