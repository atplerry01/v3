using Whycespace.Systems.Upstream.Governance.Proposals.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Registry;
using Whycespace.Systems.Upstream.Governance.Proposals.Stores;

namespace Whycespace.GovernanceProposals.Tests;

public class GovernanceProposalRegistryTests
{
    private readonly GovernanceProposalStore _store = new();
    private readonly GovernanceProposalRegistry _registry;

    public GovernanceProposalRegistryTests()
    {
        _registry = new GovernanceProposalRegistry(_store);
    }

    private GovernanceProposalRecord CreateRecord(
        Guid? proposalId = null,
        string title = "Test Proposal",
        string description = "Test Description",
        GovernanceProposalType type = GovernanceProposalType.PolicyChange,
        GovernanceProposalStatus status = GovernanceProposalStatus.Draft,
        string domain = "test-domain",
        Guid? guardianId = null)
    {
        return new GovernanceProposalRecord(
            proposalId ?? Guid.NewGuid(),
            title,
            description,
            type,
            status,
            domain,
            guardianId ?? Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            null,
            null,
            new Dictionary<string, string>());
    }

    [Fact]
    public void RegisterProposal_ValidRecord_Succeeds()
    {
        var record = CreateRecord();

        _registry.RegisterProposal(record);

        var result = _registry.GetProposal(record.ProposalId);
        Assert.NotNull(result);
        Assert.Equal(record.ProposalId, result.ProposalId);
        Assert.Equal("Test Proposal", result.ProposalTitle);
    }

    [Fact]
    public void RegisterProposal_DuplicateId_Throws()
    {
        var id = Guid.NewGuid();
        _registry.RegisterProposal(CreateRecord(proposalId: id));

        Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterProposal(CreateRecord(proposalId: id)));
    }

    [Fact]
    public void RegisterProposal_EmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.RegisterProposal(CreateRecord(title: "")));
    }

    [Fact]
    public void RegisterProposal_EmptyDomain_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.RegisterProposal(CreateRecord(domain: "")));
    }

    [Fact]
    public void RegisterProposal_EmptyGuardianId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _registry.RegisterProposal(CreateRecord(guardianId: Guid.Empty)));
    }

    [Fact]
    public void GetProposal_NotFound_ReturnsNull()
    {
        var result = _registry.GetProposal(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void GetProposal_Exists_ReturnsRecord()
    {
        var record = CreateRecord();
        _registry.RegisterProposal(record);

        var result = _registry.GetProposal(record.ProposalId);

        Assert.NotNull(result);
        Assert.Equal(record.ProposalTitle, result.ProposalTitle);
    }

    [Fact]
    public void GetProposals_ReturnsAll()
    {
        _registry.RegisterProposal(CreateRecord());
        _registry.RegisterProposal(CreateRecord());
        _registry.RegisterProposal(CreateRecord());

        var results = _registry.GetProposals();

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void GetProposalsByStatus_FiltersCorrectly()
    {
        _registry.RegisterProposal(CreateRecord(status: GovernanceProposalStatus.Draft));
        _registry.RegisterProposal(CreateRecord(status: GovernanceProposalStatus.Draft));
        _registry.RegisterProposal(CreateRecord(status: GovernanceProposalStatus.Submitted));

        var drafts = _registry.GetProposalsByStatus(GovernanceProposalStatus.Draft);
        var submitted = _registry.GetProposalsByStatus(GovernanceProposalStatus.Submitted);

        Assert.Equal(2, drafts.Count);
        Assert.Single(submitted);
    }

    [Fact]
    public void GetProposalsByType_FiltersCorrectly()
    {
        _registry.RegisterProposal(CreateRecord(type: GovernanceProposalType.PolicyChange));
        _registry.RegisterProposal(CreateRecord(type: GovernanceProposalType.PolicyChange));
        _registry.RegisterProposal(CreateRecord(type: GovernanceProposalType.EmergencyAction));

        var policyChanges = _registry.GetProposalsByType(GovernanceProposalType.PolicyChange);
        var emergencies = _registry.GetProposalsByType(GovernanceProposalType.EmergencyAction);

        Assert.Equal(2, policyChanges.Count);
        Assert.Single(emergencies);
    }

    [Fact]
    public void GetProposalsByDomain_FiltersCorrectly()
    {
        _registry.RegisterProposal(CreateRecord(domain: "finance"));
        _registry.RegisterProposal(CreateRecord(domain: "finance"));
        _registry.RegisterProposal(CreateRecord(domain: "operations"));

        var finance = _registry.GetProposalsByDomain("finance");
        var ops = _registry.GetProposalsByDomain("operations");

        Assert.Equal(2, finance.Count);
        Assert.Single(ops);
    }

    [Fact]
    public void UpdateProposalStatus_ValidTransition_Succeeds()
    {
        var record = CreateRecord(status: GovernanceProposalStatus.Draft);
        _registry.RegisterProposal(record);

        _registry.UpdateProposalStatus(record.ProposalId, GovernanceProposalStatus.Submitted);

        var updated = _registry.GetProposal(record.ProposalId);
        Assert.NotNull(updated);
        Assert.Equal(GovernanceProposalStatus.Submitted, updated.ProposalStatus);
    }

    [Fact]
    public void UpdateProposalStatus_NotFound_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _registry.UpdateProposalStatus(Guid.NewGuid(), GovernanceProposalStatus.Approved));
    }
}
