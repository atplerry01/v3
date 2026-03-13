using Whycespace.Engines.T0U.Governance;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDisputeEngineTests
{
    private readonly GovernanceDisputeStore _disputeStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceDisputeEngine _engine;

    public GovernanceDisputeEngineTests()
    {
        _engine = new GovernanceDisputeEngine(_disputeStore, _proposalStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());

        var registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        registryEngine.CreateProposal("p-1", "Proposal", "Desc", ProposalType.Policy, "g-alice");
    }

    [Fact]
    public void OpenDispute_Succeeds()
    {
        var dispute = _engine.OpenDispute("d-1", "p-1", "g-alice", "Procedure was not followed");

        Assert.Equal("d-1", dispute.DisputeId);
        Assert.Equal("p-1", dispute.ProposalId);
        Assert.Equal("g-alice", dispute.FiledBy);
        Assert.Equal(DisputeStatus.Open, dispute.Status);
        Assert.Equal(0, dispute.EscalationLevel);
        Assert.Null(dispute.ResolvedAt);
    }

    [Fact]
    public void OpenDispute_InvalidProposal_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.OpenDispute("d-bad", "nonexistent", "g-alice", "Reason"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    [Fact]
    public void OpenDispute_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.OpenDispute("d-bad", "p-1", "nonexistent", "Reason"));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void OpenDispute_EmptyReason_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.OpenDispute("d-bad", "p-1", "g-alice", ""));
        Assert.Contains("reason is required", ex.Message);
    }

    [Fact]
    public void ResolveDispute_Succeeds()
    {
        _engine.OpenDispute("d-res", "p-1", "g-alice", "Reason");

        var resolved = _engine.ResolveDispute("d-res");

        Assert.Equal(DisputeStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public void ResolveDispute_AlreadyResolved_Throws()
    {
        _engine.OpenDispute("d-dup", "p-1", "g-alice", "Reason");
        _engine.ResolveDispute("d-dup");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.ResolveDispute("d-dup"));
        Assert.Contains("already resolved", ex.Message);
    }

    [Fact]
    public void EscalateDispute_Succeeds()
    {
        _engine.OpenDispute("d-esc", "p-1", "g-alice", "Reason");

        var escalated = _engine.EscalateDispute("d-esc");

        Assert.Equal(DisputeStatus.Escalated, escalated.Status);
        Assert.Equal(1, escalated.EscalationLevel);
    }

    [Fact]
    public void EscalateDispute_MultipleTimes_IncrementsLevel()
    {
        _engine.OpenDispute("d-multi", "p-1", "g-alice", "Reason");

        _engine.EscalateDispute("d-multi");
        var escalated = _engine.EscalateDispute("d-multi");

        Assert.Equal(2, escalated.EscalationLevel);
    }

    [Fact]
    public void EscalateDispute_Resolved_Throws()
    {
        _engine.OpenDispute("d-done", "p-1", "g-alice", "Reason");
        _engine.ResolveDispute("d-done");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.EscalateDispute("d-done"));
        Assert.Contains("Cannot escalate a resolved dispute", ex.Message);
    }

    [Fact]
    public void ResolveDispute_AfterEscalation_Succeeds()
    {
        _engine.OpenDispute("d-escres", "p-1", "g-alice", "Reason");
        _engine.EscalateDispute("d-escres");

        var resolved = _engine.ResolveDispute("d-escres");

        Assert.Equal(DisputeStatus.Resolved, resolved.Status);
        Assert.Equal(1, resolved.EscalationLevel);
    }
}
