namespace Whycespace.Systems.Downstream.Cwg.Governance;

public sealed class ParticipationRules
{
    public bool IsEligible(Guid identityId, string role, decimal trustScore)
    {
        if (identityId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(role))
            return false;

        return role switch
        {
            "Administrator" => trustScore >= 80m,
            "Contributor" => trustScore >= 20m,
            "Observer" => true,
            _ => false
        };
    }

    public decimal GetMaximumAllocation(string role)
    {
        return role switch
        {
            "Administrator" => 100m,
            "Contributor" => 50m,
            "Observer" => 0m,
            _ => 0m
        };
    }
}
