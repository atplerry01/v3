using Whycespace.Systems.Upstream.Governance.Proposals.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Registry;
using Whycespace.Systems.Upstream.Governance.Proposals.Stores;

namespace Whycespace.GovernanceProposals.Tests;

public class GovernanceProposalConcurrencyTests
{
    private GovernanceProposalRecord CreateRecord(
        Guid? proposalId = null,
        string domain = "test-domain")
    {
        return new GovernanceProposalRecord(
            proposalId ?? Guid.NewGuid(),
            "Test Proposal",
            "Test Description",
            GovernanceProposalType.PolicyChange,
            GovernanceProposalStatus.Draft,
            domain,
            Guid.NewGuid(),
            DateTime.UtcNow,
            null,
            null,
            null,
            new Dictionary<string, string>());
    }

    [Fact]
    public void ConcurrentRegistration_UniqueIds_AllSucceed()
    {
        var store = new GovernanceProposalStore();
        var registry = new GovernanceProposalRegistry(store);
        var records = Enumerable.Range(0, 100).Select(_ => CreateRecord()).ToList();

        Parallel.ForEach(records, record => registry.RegisterProposal(record));

        Assert.Equal(100, registry.GetProposals().Count);
    }

    [Fact]
    public void ConcurrentRegistration_DuplicateId_OnlyOneSucceeds()
    {
        var store = new GovernanceProposalStore();
        var registry = new GovernanceProposalRegistry(store);
        var sharedId = Guid.NewGuid();
        var exceptions = new global::System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.For(0, 10, _ =>
        {
            try
            {
                registry.RegisterProposal(CreateRecord(proposalId: sharedId));
            }
            catch (InvalidOperationException ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Equal(1, registry.GetProposals().Count);
        Assert.Equal(9, exceptions.Count);
    }

    [Fact]
    public void ConcurrentReads_DoNotThrow()
    {
        var store = new GovernanceProposalStore();
        var registry = new GovernanceProposalRegistry(store);

        for (int i = 0; i < 50; i++)
            registry.RegisterProposal(CreateRecord());

        Parallel.For(0, 100, _ =>
        {
            var all = registry.GetProposals();
            Assert.Equal(50, all.Count);

            var drafts = registry.GetProposalsByStatus(GovernanceProposalStatus.Draft);
            Assert.Equal(50, drafts.Count);

            var policies = registry.GetProposalsByType(GovernanceProposalType.PolicyChange);
            Assert.Equal(50, policies.Count);

            var byDomain = registry.GetProposalsByDomain("test-domain");
            Assert.Equal(50, byDomain.Count);
        });
    }
}
