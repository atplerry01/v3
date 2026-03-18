namespace Whycespace.Systems.Downstream.Spv.Governance;

public sealed class SpvGovernancePolicy
{
    public bool CanVote(Guid identityId, Guid spvId, decimal ownershipPercentage)
    {
        if (identityId == Guid.Empty || spvId == Guid.Empty)
            return false;

        return ownershipPercentage > 0m;
    }

    public decimal GetVotingWeight(decimal ownershipPercentage)
        => ownershipPercentage;

    public bool HasQuorum(decimal totalVotingWeight)
        => totalVotingWeight >= 51m;
}
