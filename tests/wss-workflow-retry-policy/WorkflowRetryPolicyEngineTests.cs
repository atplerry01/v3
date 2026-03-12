using Whycespace.Engines.T1M.WSS.Runtime;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Models;

namespace Whycespace.WSS.WorkflowRetryPolicy.Tests;

public class WorkflowRetryPolicyEngineTests
{
    private readonly WorkflowRetryPolicyEngine _engine;
    private readonly WorkflowRetryStore _store;

    public WorkflowRetryPolicyEngineTests()
    {
        _store = new WorkflowRetryStore();
        _engine = new WorkflowRetryPolicyEngine(_store);
    }

    private static WorkflowFailurePolicy RetryPolicy(int maxRetries = 3, int delaySeconds = 5) =>
        new(FailureAction.Retry, maxRetries, TimeSpan.FromSeconds(delaySeconds), null);

    // 1. Retry allowed within limit
    [Fact]
    public void EvaluateRetryPolicy_WithinLimit_ShouldAllowRetry()
    {
        var policy = RetryPolicy(maxRetries: 3);

        var decision = _engine.EvaluateRetryPolicy(policy, currentRetryCount: 1);

        Assert.True(decision.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(5), decision.RetryDelay);
        Assert.Equal(FailureAction.Retry, decision.FailureAction);
    }

    // 2. Retry blocked after max retries
    [Fact]
    public void EvaluateRetryPolicy_ExceedsMaxRetries_ShouldBlockRetry()
    {
        var policy = RetryPolicy(maxRetries: 3);

        var decision = _engine.EvaluateRetryPolicy(policy, currentRetryCount: 3);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(TimeSpan.Zero, decision.RetryDelay);
        Assert.Equal(FailureAction.Fail, decision.FailureAction);
    }

    // 3. Retry counter increment
    [Fact]
    public void RegisterRetryAttempt_ShouldIncrementCounter()
    {
        _engine.RegisterRetryAttempt("inst-1", "step-validate");
        _engine.RegisterRetryAttempt("inst-1", "step-validate");
        _engine.RegisterRetryAttempt("inst-1", "step-validate");

        var count = _engine.GetRetryCount("inst-1", "step-validate");

        Assert.Equal(3, count);
    }

    // 4. Retry counter reset
    [Fact]
    public void ResetRetryCount_ShouldClearCounter()
    {
        _engine.RegisterRetryAttempt("inst-2", "step-match");
        _engine.RegisterRetryAttempt("inst-2", "step-match");

        _engine.ResetRetryCount("inst-2", "step-match");

        var count = _engine.GetRetryCount("inst-2", "step-match");
        Assert.Equal(0, count);
    }

    // 5. Skip policy handling
    [Fact]
    public void EvaluateRetryPolicy_SkipAction_ShouldNotRetry()
    {
        var policy = new WorkflowFailurePolicy(FailureAction.Skip, 0, TimeSpan.Zero, null);

        var decision = _engine.EvaluateRetryPolicy(policy, currentRetryCount: 0);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(FailureAction.Skip, decision.FailureAction);
    }

    // 6. Fail policy handling
    [Fact]
    public void EvaluateRetryPolicy_FailAction_ShouldNotRetry()
    {
        var policy = new WorkflowFailurePolicy(FailureAction.Fail, 0, TimeSpan.Zero, null);

        var decision = _engine.EvaluateRetryPolicy(policy, currentRetryCount: 0);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(FailureAction.Fail, decision.FailureAction);
    }
}
