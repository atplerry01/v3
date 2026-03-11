namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class QuorumEngine
{
    private readonly GovernanceVoteStore _voteStore;
    private readonly GuardianRegistryStore _guardianStore;
    private readonly QuorumConfig _config;

    public QuorumEngine(
        GovernanceVoteStore voteStore,
        GuardianRegistryStore guardianStore,
        QuorumConfig config)
    {
        _voteStore = voteStore;
        _guardianStore = guardianStore;
        _config = config;
    }

    public bool CheckQuorum(string proposalId)
    {
        var votes = _voteStore.GetByProposal(proposalId);
        var threshold = CalculateQuorumThreshold();

        return votes.Count >= threshold;
    }

    public int CalculateQuorumThreshold()
    {
        var activeGuardians = _guardianStore.ListGuardians()
            .Count(g => g.Status == GuardianStatus.Active);

        if (activeGuardians == 0)
            return 0;

        return (int)Math.Ceiling(activeGuardians * _config.ThresholdPercent / 100.0);
    }
}
