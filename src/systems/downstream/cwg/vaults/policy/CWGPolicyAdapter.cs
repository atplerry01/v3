namespace Whycespace.Systems.Downstream.Cwg.Vaults.Policy;

public sealed class CWGPolicyAdapter
{
    public bool ValidateContribution(Guid participantId, decimal amount, string vaultPurpose)
    {
        if (participantId == Guid.Empty)
            return false;

        if (amount <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(vaultPurpose))
            return false;

        return true;
    }

    public bool ValidateAllocation(Guid vaultId, Guid recipientId, decimal percentage)
    {
        if (vaultId == Guid.Empty || recipientId == Guid.Empty)
            return false;

        if (percentage is <= 0 or > 100)
            return false;

        return true;
    }
}
