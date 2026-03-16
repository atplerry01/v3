using Whycespace.Systems.Upstream.Governance.Proposals.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Stores;

namespace Whycespace.GovernanceProposals.Tests;

public class GovernanceProposalStoreTests
{
    private readonly GovernanceProposalStore _store = new();

    private GovernanceProposalRecord CreateRecord(
        Guid? proposalId = null,
        GovernanceProposalStatus status = GovernanceProposalStatus.Draft,
        GovernanceProposalType type = GovernanceProposalType.PolicyChange,
        string domain = "test-domain")
    {
        return new GovernanceProposalRecord(
            proposalId ?? Guid.NewGuid(),
            "Test Proposal",
            "Test Description",
            type,
            status,
            domain,
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            null,
            null,
            new Dictionary<string, string>());
    }

    [Fact]
    public void Add_ValidRecord_Succeeds()
    {
        var record = CreateRecord();

        _store.Add(record);

        Assert.True(_store.Exists(record.ProposalId));
    }

    [Fact]
    public void Add_DuplicateId_Throws()
    {
        var id = Guid.NewGuid();
        _store.Add(CreateRecord(proposalId: id));

        Assert.Throws<InvalidOperationException>(() =>
            _store.Add(CreateRecord(proposalId: id)));
    }

    [Fact]
    public void Get_Exists_ReturnsRecord()
    {
        var record = CreateRecord();
        _store.Add(record);

        var result = _store.Get(record.ProposalId);

        Assert.NotNull(result);
        Assert.Equal(record.ProposalId, result.ProposalId);
    }

    [Fact]
    public void Get_NotFound_ReturnsNull()
    {
        var result = _store.Get(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Update_Exists_Succeeds()
    {
        var record = CreateRecord();
        _store.Add(record);

        var updated = record with { ProposalStatus = GovernanceProposalStatus.Approved };
        _store.Update(updated);

        var result = _store.Get(record.ProposalId);
        Assert.NotNull(result);
        Assert.Equal(GovernanceProposalStatus.Approved, result.ProposalStatus);
    }

    [Fact]
    public void Update_NotFound_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            _store.Update(CreateRecord()));
    }

    [Fact]
    public void ListAll_ReturnsAllRecords()
    {
        _store.Add(CreateRecord());
        _store.Add(CreateRecord());

        var all = _store.ListAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void ListByStatus_FiltersCorrectly()
    {
        _store.Add(CreateRecord(status: GovernanceProposalStatus.Draft));
        _store.Add(CreateRecord(status: GovernanceProposalStatus.Approved));

        var drafts = _store.ListByStatus(GovernanceProposalStatus.Draft);

        Assert.Single(drafts);
        Assert.Equal(GovernanceProposalStatus.Draft, drafts[0].ProposalStatus);
    }

    [Fact]
    public void ListByType_FiltersCorrectly()
    {
        _store.Add(CreateRecord(type: GovernanceProposalType.PolicyChange));
        _store.Add(CreateRecord(type: GovernanceProposalType.SystemUpgrade));

        var policies = _store.ListByType(GovernanceProposalType.PolicyChange);

        Assert.Single(policies);
        Assert.Equal(GovernanceProposalType.PolicyChange, policies[0].ProposalType);
    }

    [Fact]
    public void ListByDomain_FiltersCorrectly()
    {
        _store.Add(CreateRecord(domain: "finance"));
        _store.Add(CreateRecord(domain: "operations"));

        var finance = _store.ListByDomain("finance");

        Assert.Single(finance);
        Assert.Equal("finance", finance[0].AuthorityDomain);
    }

    [Fact]
    public void Exists_Found_ReturnsTrue()
    {
        var record = CreateRecord();
        _store.Add(record);

        Assert.True(_store.Exists(record.ProposalId));
    }

    [Fact]
    public void Exists_NotFound_ReturnsFalse()
    {
        Assert.False(_store.Exists(Guid.NewGuid()));
    }
}
