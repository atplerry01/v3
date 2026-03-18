using Whycespace.Reliability.Retry;

namespace Whycespace.Reliability.Tests;

public class RetryPolicyTests
{
    [Fact]
    public void ShouldRetry_Returns_True_When_Under_MaxRetries()
    {
        var policy = new RetryPolicy(maxRetries: 3);

        Assert.True(policy.ShouldRetry(0));
        Assert.True(policy.ShouldRetry(1));
        Assert.True(policy.ShouldRetry(2));
    }

    [Fact]
    public void ShouldRetry_Returns_False_When_At_MaxRetries()
    {
        var policy = new RetryPolicy(maxRetries: 3);
        Assert.False(policy.ShouldRetry(3));
    }

    [Fact]
    public void GetDelay_Returns_Exponential_Backoff()
    {
        var policy = new RetryPolicy(maxRetries: 3, initialDelay: TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(1), policy.GetDelay(0));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.GetDelay(2));
    }
}
