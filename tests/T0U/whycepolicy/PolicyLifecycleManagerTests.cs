using Whycespace.Engines.T0U.WhycePolicy.Lifecycle.Engines;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyLifecycleManagerTests
{
    private static PolicyLifecycleManager CreateEngine()
    {
        var store = new PolicyLifecycleStore();
        return new PolicyLifecycleManager(store);
    }

    private static PolicyLifecycleManager CreateEngineWithDraftPolicy(
        PolicyLifecycleStore store,
        string policyId = "policy-1",
        string version = "1")
    {
        var record = new PolicyLifecycleRecord(policyId, version, PolicyLifecycleState.Draft, DateTime.UtcNow);
        store.SetLifecycleState(record);
        return new PolicyLifecycleManager(store);
    }

    [Fact]
    public void ApprovePolicy_FromDraft_TransitionsToApproved()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);

        var result = engine.ApprovePolicy("policy-1", "1");

        Assert.Equal(PolicyLifecycleState.Approved, result.State);
        Assert.Equal("policy-1", result.PolicyId);
        Assert.Equal("1", result.Version);
    }

    [Fact]
    public void ActivatePolicy_FromApproved_TransitionsToActive()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");

        var result = engine.ActivatePolicy("policy-1", "1");

        Assert.Equal(PolicyLifecycleState.Active, result.State);
    }

    [Fact]
    public void DeprecatePolicy_FromActive_TransitionsToDeprecated()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");
        engine.ActivatePolicy("policy-1", "1");

        var result = engine.DeprecatePolicy("policy-1", "1");

        Assert.Equal(PolicyLifecycleState.Deprecated, result.State);
    }

    [Fact]
    public void ArchivePolicy_FromDeprecated_TransitionsToArchived()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");
        engine.ActivatePolicy("policy-1", "1");
        engine.DeprecatePolicy("policy-1", "1");

        var result = engine.ArchivePolicy("policy-1", "1");

        Assert.Equal(PolicyLifecycleState.Archived, result.State);
    }

    [Fact]
    public void InvalidTransition_DraftToActive_ThrowsInvalidOperation()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);

        Assert.Throws<InvalidOperationException>(() =>
            engine.ActivatePolicy("policy-1", "1"));
    }

    [Fact]
    public void InvalidTransition_ArchivedToActive_ThrowsInvalidOperation()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");
        engine.ActivatePolicy("policy-1", "1");
        engine.DeprecatePolicy("policy-1", "1");
        engine.ArchivePolicy("policy-1", "1");

        Assert.Throws<InvalidOperationException>(() =>
            engine.ActivatePolicy("policy-1", "1"));
    }

    [Fact]
    public void GetLifecycleState_ReturnsCurrentState()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");

        var record = engine.GetLifecycleState("policy-1", "1");

        Assert.Equal(PolicyLifecycleState.Approved, record.State);
    }

    [Fact]
    public void GetLifecycleState_NotFound_ThrowsKeyNotFound()
    {
        var engine = CreateEngine();

        Assert.Throws<KeyNotFoundException>(() =>
            engine.GetLifecycleState("nonexistent", "1"));
    }

    [Fact]
    public void GetLifecycleHistory_ReturnsAllTransitions()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);
        engine.ApprovePolicy("policy-1", "1");
        engine.ActivatePolicy("policy-1", "1");

        var history = engine.GetLifecycleHistory("policy-1", "1");

        Assert.Equal(3, history.Count); // Draft + Approved + Active
    }

    [Fact]
    public void FullLifecycle_DraftToArchived_TransitionsCorrectly()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store);

        engine.ApprovePolicy("policy-1", "1");
        engine.ActivatePolicy("policy-1", "1");
        engine.DeprecatePolicy("policy-1", "1");
        engine.ArchivePolicy("policy-1", "1");

        var final = engine.GetLifecycleState("policy-1", "1");
        Assert.Equal(PolicyLifecycleState.Archived, final.State);
    }

    [Fact]
    public void PolicyId_PreservedInResult()
    {
        var store = new PolicyLifecycleStore();
        var engine = CreateEngineWithDraftPolicy(store, "custom-policy-id", "1");

        var result = engine.ApprovePolicy("custom-policy-id", "1");

        Assert.Equal("custom-policy-id", result.PolicyId);
    }

    [Fact]
    public void ConcurrentAccess_ProducesConsistentResults()
    {
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            return Task.Run(() =>
            {
                var policyId = $"policy-{i}";
                var store = new PolicyLifecycleStore();
                var record = new PolicyLifecycleRecord(policyId, "1", PolicyLifecycleState.Draft, DateTime.UtcNow);
                store.SetLifecycleState(record);
                var engine = new PolicyLifecycleManager(store);

                engine.ApprovePolicy(policyId, "1");
                engine.ActivatePolicy(policyId, "1");

                var state = engine.GetLifecycleState(policyId, "1");
                return state.State;
            });
        }).ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            Assert.Equal(PolicyLifecycleState.Active, task.Result);
        }
    }
}
