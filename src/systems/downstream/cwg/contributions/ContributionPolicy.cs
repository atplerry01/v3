namespace Whycespace.Systems.Downstream.Cwg.Contributions;

public sealed class ContributionPolicy
{
    public bool CanContribute(Guid participantId, Guid vaultId, decimal amount)
    {
        if (participantId == Guid.Empty || vaultId == Guid.Empty)
            return false;

        if (amount <= 0)
            return false;

        return true;
    }

    public decimal GetMinimumContribution(string contributionType)
    {
        return contributionType switch
        {
            "Capital" => 100m,
            "Labour" => 0m,
            "Resource" => 10m,
            _ => 0m
        };
    }
}
