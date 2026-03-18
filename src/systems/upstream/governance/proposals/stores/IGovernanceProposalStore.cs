namespace Whycespace.Systems.Upstream.Governance.Proposals.Stores;

using Whycespace.Systems.Upstream.Governance.Proposals.Models;

public interface IGovernanceProposalStore
{
    void Add(GovernanceProposalRecord proposal);

    GovernanceProposalRecord? Get(Guid proposalId);

    void Update(GovernanceProposalRecord proposal);

    IReadOnlyList<GovernanceProposalRecord> ListAll();

    IReadOnlyList<GovernanceProposalRecord> ListByStatus(GovernanceProposalStatus status);

    IReadOnlyList<GovernanceProposalRecord> ListByType(GovernanceProposalType type);

    IReadOnlyList<GovernanceProposalRecord> ListByDomain(string domain);

    bool Exists(Guid proposalId);
}
