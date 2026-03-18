namespace Whycespace.Tests.Runtime;

using Whycespace.Runtime.Reliability;
using Xunit;

public sealed class RetryPolicyEngineTests
{
    [Fact]
    public async Task SuccessOnFirstAttempt_DoesNotRetry()
    {
        var engine = new RetryPolicyEngine { MaxRetries = 3, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var attempts = 0;

        var result = await engine.ExecuteWithRetryAsync(
            async () => { attempts++; return await Task.FromResult("ok"); },
            r => r != "ok");

        Assert.Equal("ok", result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task FailsThenSucceeds_RetriesCorrectly()
    {
        var engine = new RetryPolicyEngine { MaxRetries = 3, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var attempts = 0;

        var result = await engine.ExecuteWithRetryAsync(
            async () => { attempts++; return await Task.FromResult(attempts >= 3 ? "ok" : "fail"); },
            r => r != "ok");

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExceedsMaxRetries_ReturnsLastResult()
    {
        var engine = new RetryPolicyEngine { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var attempts = 0;

        var result = await engine.ExecuteWithRetryAsync(
            async () => { attempts++; return await Task.FromResult("fail"); },
            r => r == "fail");

        Assert.Equal("fail", result);
        Assert.Equal(3, attempts); // initial + 2 retries
    }

    [Fact]
    public async Task ExponentialBackoff_DelaysIncrease()
    {
        var engine = new RetryPolicyEngine { MaxRetries = 2, InitialDelay = TimeSpan.FromMilliseconds(50) };
        var timestamps = new List<DateTimeOffset>();

        await engine.ExecuteWithRetryAsync(
            async () =>
            {
                timestamps.Add(DateTimeOffset.UtcNow);
                return await Task.FromResult("fail");
            },
            r => r == "fail");

        Assert.Equal(3, timestamps.Count);
        // Second delay should be roughly double the first
        var firstGap = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var secondGap = (timestamps[2] - timestamps[1]).TotalMilliseconds;
        Assert.True(secondGap > firstGap * 1.5, $"Expected exponential backoff: first={firstGap}ms, second={secondGap}ms");
    }
}
