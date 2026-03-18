using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

namespace Whycespace.WhycePolicy.Tests;

public class PolicyLifecycleManagerTests
{
    private static PolicyLifecycleManager CreateEngine() => new();

    private static PolicyLifecycleCommand MakeCommand(
        PolicyLifecycleState current,
        PolicyLifecycleState target,
        string policyId = "policy-1",
        string requestedBy = "admin",
        string reason = "test transition") =>
        new(policyId, current, target, requestedBy, reason);

    [Fact]
    public void DraftToReview_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Draft, PolicyLifecycleState.Review));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Draft, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Review, result.NewState);
    }

    [Fact]
    public void ReviewToApproved_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Review, PolicyLifecycleState.Approved));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Review, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Approved, result.NewState);
    }

    [Fact]
    public void ApprovedToActive_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Approved, PolicyLifecycleState.Active));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Approved, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Active, result.NewState);
    }

    [Fact]
    public void ActiveToSuspended_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Active, PolicyLifecycleState.Suspended));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Active, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Suspended, result.NewState);
    }

    [Fact]
    public void ActiveToRevoked_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Active, PolicyLifecycleState.Revoked));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Active, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Revoked, result.NewState);
    }

    [Fact]
    public void SuspendedToActive_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Suspended, PolicyLifecycleState.Active));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Suspended, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Active, result.NewState);
    }

    [Fact]
    public void SuspendedToRevoked_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Suspended, PolicyLifecycleState.Revoked));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Suspended, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Revoked, result.NewState);
    }

    [Fact]
    public void RevokedToArchived_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Revoked, PolicyLifecycleState.Archived));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Revoked, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Archived, result.NewState);
    }

    [Fact]
    public void ReviewToRejected_ValidTransition_ReturnsAllowed()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Review, PolicyLifecycleState.Rejected));

        Assert.True(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Review, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Rejected, result.NewState);
    }

    [Fact]
    public void InvalidTransition_DraftToActive_ReturnsRejected()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Draft, PolicyLifecycleState.Active));

        Assert.False(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Draft, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Draft, result.NewState);
        Assert.Contains("not allowed", result.TransitionReason);
    }

    [Fact]
    public void ArchivedState_NoFurtherTransitions_ReturnsRejected()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Archived, PolicyLifecycleState.Draft));

        Assert.False(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Archived, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Archived, result.NewState);
    }

    [Fact]
    public void RejectedState_NoFurtherTransitions_ReturnsRejected()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(PolicyLifecycleState.Rejected, PolicyLifecycleState.Active));

        Assert.False(result.TransitionAllowed);
        Assert.Equal(PolicyLifecycleState.Rejected, result.PreviousState);
        Assert.Equal(PolicyLifecycleState.Rejected, result.NewState);
    }

    [Fact]
    public void DeterministicTransitionValidation_SameInputSameOutput()
    {
        var engine = CreateEngine();
        var command = MakeCommand(PolicyLifecycleState.Draft, PolicyLifecycleState.Review);

        var result1 = engine.ProcessTransition(command);
        var result2 = engine.ProcessTransition(command);

        Assert.Equal(result1.TransitionAllowed, result2.TransitionAllowed);
        Assert.Equal(result1.PreviousState, result2.PreviousState);
        Assert.Equal(result1.NewState, result2.NewState);
        Assert.Equal(result1.PolicyId, result2.PolicyId);
    }

    [Fact]
    public void ConcurrentSafety_ProducesConsistentResults()
    {
        var engine = CreateEngine();
        var command = MakeCommand(PolicyLifecycleState.Active, PolicyLifecycleState.Suspended);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => engine.ProcessTransition(command)))
            .ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            Assert.True(task.Result.TransitionAllowed);
            Assert.Equal(PolicyLifecycleState.Suspended, task.Result.NewState);
        }
    }

    [Fact]
    public void PolicyId_PreservedInResult()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(
            PolicyLifecycleState.Draft, PolicyLifecycleState.Review, policyId: "custom-policy-id"));

        Assert.Equal("custom-policy-id", result.PolicyId);
    }

    [Fact]
    public void TransitionReason_IncludesRequestedByAndReason()
    {
        var engine = CreateEngine();
        var result = engine.ProcessTransition(MakeCommand(
            PolicyLifecycleState.Draft, PolicyLifecycleState.Review,
            requestedBy: "governance-admin", reason: "initial review submission"));

        Assert.True(result.TransitionAllowed);
        Assert.Contains("governance-admin", result.TransitionReason);
        Assert.Contains("initial review submission", result.TransitionReason);
    }
}
