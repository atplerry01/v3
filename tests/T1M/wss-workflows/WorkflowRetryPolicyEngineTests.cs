using Whycespace.Engines.T1M.Shared;
using Whycespace.Engines.T1M.Orchestration.Resilience;

namespace Whycespace.WSS.Workflows.Tests;

public sealed class WorkflowRetryPolicyEngineTests
{
    // --- Helpers ---

    private static WorkflowRetryPolicyCommand Command(
        string instanceId,
        string stepId,
        RetryPolicy policy,
        int currentRetryCount,
        DateTimeOffset? lastFailure = null)
        => new(instanceId, stepId, policy, currentRetryCount,
            lastFailure ?? DateTimeOffset.UtcNow);

    private static RetryPolicy ImmediatePolicy(int maxRetries)
        => new(maxRetries, RetryStrategy.Immediate, TimeSpan.Zero, 1.0);

    private static RetryPolicy FixedDelayPolicy(int maxRetries, int delayMs)
        => new(maxRetries, RetryStrategy.FixedDelay, TimeSpan.FromMilliseconds(delayMs), 1.0);

    private static RetryPolicy ExponentialPolicy(int maxRetries, int initialDelayMs, double multiplier)
        => new(maxRetries, RetryStrategy.ExponentialBackoff, TimeSpan.FromMilliseconds(initialDelayMs), multiplier);

    // --- Test 1: Retry allowed within retry limit ---

    [Fact]
    public void EvaluateRetryPolicy_WithinLimit_RetryAllowed()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(3, 1000), currentRetryCount: 1);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.True(result.RetryAllowed);
        Assert.Equal("wf-1", result.WorkflowInstanceId);
        Assert.Equal("step-1", result.StepId);
        Assert.Equal(1, result.RetryCount);
    }

    [Fact]
    public void EvaluateRetryPolicy_ZeroRetries_RetryAllowed()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(3, 1000), currentRetryCount: 0);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.True(result.RetryAllowed);
    }

    // --- Test 2: Retry denied after max retries ---

    [Fact]
    public void EvaluateRetryPolicy_AtMaxRetries_RetryDenied()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(3, 1000), currentRetryCount: 3);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.False(result.RetryAllowed);
        Assert.Equal(TimeSpan.Zero, result.RetryDelay);
    }

    [Fact]
    public void EvaluateRetryPolicy_ExceedsMaxRetries_RetryDenied()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(3, 1000), currentRetryCount: 5);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.False(result.RetryAllowed);
    }

    [Fact]
    public void EvaluateRetryPolicy_ZeroMaxRetries_RetryDenied()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(0, 1000), currentRetryCount: 0);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.False(result.RetryAllowed);
    }

    // --- Test 3: Fixed delay retry calculation ---

    [Fact]
    public void EvaluateRetryPolicy_FixedDelay_ReturnsConstantDelay()
    {
        var command0 = Command("wf-1", "step-1", FixedDelayPolicy(5, 2000), currentRetryCount: 0);
        var command1 = Command("wf-1", "step-1", FixedDelayPolicy(5, 2000), currentRetryCount: 1);
        var command3 = Command("wf-1", "step-1", FixedDelayPolicy(5, 2000), currentRetryCount: 3);

        var result0 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command0);
        var result1 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command1);
        var result3 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command3);

        Assert.Equal(TimeSpan.FromMilliseconds(2000), result0.RetryDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(2000), result1.RetryDelay);
        Assert.Equal(TimeSpan.FromMilliseconds(2000), result3.RetryDelay);
    }

    // --- Test 4: Exponential backoff calculation ---

    [Fact]
    public void EvaluateRetryPolicy_ExponentialBackoff_CalculatesCorrectDelay()
    {
        // Delay = InitialDelay * (BackoffMultiplier ^ RetryCount)
        var policy = ExponentialPolicy(5, 1000, 2.0);

        var r0 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(Command("wf-1", "s1", policy, 0));
        var r1 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(Command("wf-1", "s1", policy, 1));
        var r2 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(Command("wf-1", "s1", policy, 2));
        var r3 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(Command("wf-1", "s1", policy, 3));

        // 1000 * 2^0 = 1000ms
        Assert.Equal(TimeSpan.FromMilliseconds(1000), r0.RetryDelay);
        // 1000 * 2^1 = 2000ms
        Assert.Equal(TimeSpan.FromMilliseconds(2000), r1.RetryDelay);
        // 1000 * 2^2 = 4000ms
        Assert.Equal(TimeSpan.FromMilliseconds(4000), r2.RetryDelay);
        // 1000 * 2^3 = 8000ms
        Assert.Equal(TimeSpan.FromMilliseconds(8000), r3.RetryDelay);
    }

    [Fact]
    public void EvaluateRetryPolicy_ExponentialBackoff_CapsAtMaxDelay()
    {
        // With a very high retry count, delay should be capped at RetryPolicy.MaxDelay (30 min)
        var policy = ExponentialPolicy(100, 60000, 3.0);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(
            Command("wf-1", "s1", policy, 20));

        Assert.Equal(RetryPolicy.MaxDelay, result.RetryDelay);
    }

    // --- Test 5: Immediate retry ---

    [Fact]
    public void EvaluateRetryPolicy_ImmediateStrategy_ZeroDelay()
    {
        var command = Command("wf-1", "step-1", ImmediatePolicy(3), currentRetryCount: 1);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.True(result.RetryAllowed);
        Assert.Equal(TimeSpan.Zero, result.RetryDelay);
    }

    // --- Test 6: Concurrent retry evaluation ---

    [Fact]
    public async Task EvaluateRetryPolicy_ConcurrentCalls_IsDeterministic()
    {
        var policy = ExponentialPolicy(5, 1000, 2.0);
        var command = Command("wf-concurrent", "step-x", policy, currentRetryCount: 2);

        var results = new WorkflowRetryPolicyResult[50];
        var tasks = Enumerable.Range(0, 50).Select(i =>
            Task.Run(() =>
            {
                results[i] = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);
            })).ToArray();

        await Task.WhenAll(tasks);

        var baseline = results[0];
        foreach (var result in results)
        {
            Assert.Equal(baseline.WorkflowInstanceId, result.WorkflowInstanceId);
            Assert.Equal(baseline.StepId, result.StepId);
            Assert.Equal(baseline.RetryAllowed, result.RetryAllowed);
            Assert.Equal(baseline.RetryDelay, result.RetryDelay);
            Assert.Equal(baseline.RetryCount, result.RetryCount);
        }
    }

    // --- Test 7: Result metadata ---

    [Fact]
    public void EvaluateRetryPolicy_ResultContainsDecisionTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var command = Command("wf-1", "step-1", FixedDelayPolicy(3, 1000), currentRetryCount: 0);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.True(result.DecisionTimestamp >= before);
        Assert.True(result.DecisionTimestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void EvaluateRetryPolicy_ResultPreservesIdentifiers()
    {
        var command = Command("instance-42", "validate-step", FixedDelayPolicy(3, 1000), currentRetryCount: 1);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.Equal("instance-42", result.WorkflowInstanceId);
        Assert.Equal("validate-step", result.StepId);
    }

    // --- Test 8: Idempotency ---

    [Fact]
    public void EvaluateRetryPolicy_SameInputTwice_SameDecision()
    {
        var command = Command("wf-1", "step-1", ExponentialPolicy(3, 500, 2.0), currentRetryCount: 2);

        var r1 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);
        var r2 = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.Equal(r1.RetryAllowed, r2.RetryAllowed);
        Assert.Equal(r1.RetryDelay, r2.RetryDelay);
        Assert.Equal(r1.RetryCount, r2.RetryCount);
        Assert.Equal(r1.WorkflowInstanceId, r2.WorkflowInstanceId);
        Assert.Equal(r1.StepId, r2.StepId);
    }

    // --- Test 9: Boundary — exactly at limit minus one ---

    [Fact]
    public void EvaluateRetryPolicy_OneBeforeMax_StillAllowed()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(5, 1000), currentRetryCount: 4);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public void EvaluateRetryPolicy_ExactlyAtMax_Denied()
    {
        var command = Command("wf-1", "step-1", FixedDelayPolicy(5, 1000), currentRetryCount: 5);

        var result = WorkflowRetryPolicyEngine.EvaluateRetryPolicy(command);

        Assert.False(result.RetryAllowed);
    }
}
