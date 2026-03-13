using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyLifecycleManagerTests
{
    private readonly PolicyLifecycleStore _store = new();
    private readonly PolicyLifecycleManager _engine;

    public PolicyLifecycleManagerTests()
    {
        _engine = new PolicyLifecycleManager(_store);
    }

    private void SetDraft(string policyId, string version)
    {
        _store.SetLifecycleState(new PolicyLifecycleRecord(policyId, version, PolicyLifecycleState.Draft, DateTime.UtcNow));
    }

    [Fact]
    public void ApprovePolicy_DraftToApproved()
    {
        SetDraft("pol-1", "1");

        var result = _engine.ApprovePolicy("pol-1", "1");

        Assert.Equal(PolicyLifecycleState.Approved, result.State);
    }

    [Fact]
    public void ActivatePolicy_ApprovedToActive()
    {
        SetDraft("pol-2", "1");
        _engine.ApprovePolicy("pol-2", "1");

        var result = _engine.ActivatePolicy("pol-2", "1");

        Assert.Equal(PolicyLifecycleState.Active, result.State);
    }

    [Fact]
    public void DeprecatePolicy_ActiveToDeprecated()
    {
        SetDraft("pol-3", "1");
        _engine.ApprovePolicy("pol-3", "1");
        _engine.ActivatePolicy("pol-3", "1");

        var result = _engine.DeprecatePolicy("pol-3", "1");

        Assert.Equal(PolicyLifecycleState.Deprecated, result.State);
    }

    [Fact]
    public void ArchivePolicy_DeprecatedToArchived()
    {
        SetDraft("pol-4", "1");
        _engine.ApprovePolicy("pol-4", "1");
        _engine.ActivatePolicy("pol-4", "1");
        _engine.DeprecatePolicy("pol-4", "1");

        var result = _engine.ArchivePolicy("pol-4", "1");

        Assert.Equal(PolicyLifecycleState.Archived, result.State);
    }

    [Fact]
    public void InvalidTransition_DraftToActive_Throws()
    {
        SetDraft("pol-5", "1");

        var ex = Assert.Throws<InvalidOperationException>(() => _engine.ActivatePolicy("pol-5", "1"));
        Assert.Contains("expected 'Approved'", ex.Message);
    }

    [Fact]
    public void GetLifecycleState_ReturnsCurrentState()
    {
        SetDraft("pol-6", "1");
        _engine.ApprovePolicy("pol-6", "1");

        var state = _engine.GetLifecycleState("pol-6", "1");

        Assert.Equal(PolicyLifecycleState.Approved, state.State);
        Assert.Equal("pol-6", state.PolicyId);
    }

    [Fact]
    public void GetLifecycleHistory_ReturnsAllTransitions()
    {
        SetDraft("pol-7", "1");
        _engine.ApprovePolicy("pol-7", "1");
        _engine.ActivatePolicy("pol-7", "1");

        var history = _engine.GetLifecycleHistory("pol-7", "1");

        Assert.Equal(3, history.Count);
        Assert.Equal(PolicyLifecycleState.Draft, history[0].State);
        Assert.Equal(PolicyLifecycleState.Approved, history[1].State);
        Assert.Equal(PolicyLifecycleState.Active, history[2].State);
    }

    [Fact]
    public void MultiplePolicies_TrackedIndependently()
    {
        SetDraft("ind-a", "1");
        SetDraft("ind-b", "1");

        _engine.ApprovePolicy("ind-a", "1");

        var stateA = _engine.GetLifecycleState("ind-a", "1");
        var stateB = _engine.GetLifecycleState("ind-b", "1");

        Assert.Equal(PolicyLifecycleState.Approved, stateA.State);
        Assert.Equal(PolicyLifecycleState.Draft, stateB.State);
    }
}
