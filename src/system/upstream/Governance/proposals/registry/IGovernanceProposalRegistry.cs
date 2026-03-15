namespace Whycespace.System.Upstream.Governance.Proposals.Registry;

using Whycespace.System.Upstream.Governance.Proposals.Models;

public interface IGovernanceProposalRegistry
{
    void RegisterProposal(GovernanceProposalRecord proposal);

    GovernanceProposalRecord? GetProposal(Guid proposalId);

    IReadOnlyList<GovernanceProposalRecord> GetProposals();

    IReadOnlyList<GovernanceProposalRecord> GetProposalsByStatus(GovernanceProposalStatus status);

    IReadOnlyList<GovernanceProposalRecord> GetProposalsByType(GovernanceProposalType type);

    IReadOnlyList<GovernanceProposalRecord> GetProposalsByDomain(string domain);

    void UpdateProposalStatus(Guid proposalId, GovernanceProposalStatus status);
}
