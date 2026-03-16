namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections.Vault;

public sealed record VaultParticipantAllocationReadModel(
    Guid AllocationId,
    Guid VaultId,
    Guid ParticipantId,
    decimal AllocationPercentage,
    decimal ContributionAmount,
    decimal ProfitSharePercentage,
    DateTime AllocationTimestamp,
    DateTime LastUpdated,
    string? ParticipantRole = null,
    string? AllocationSummary = null
)
{
    public static VaultParticipantAllocationReadModel Initial(
        Guid allocationId, Guid vaultId, Guid participantId, DateTime timestamp) =>
        new(allocationId, vaultId, participantId, 0m, 0m, 0m, timestamp, timestamp);

    public VaultParticipantAllocationReadModel WithAllocation(
        decimal percentage, decimal profitShare, DateTime timestamp) =>
        this with
        {
            AllocationPercentage = percentage,
            ProfitSharePercentage = profitShare,
            LastUpdated = timestamp,
            AllocationSummary = $"Allocation set to {percentage:F2}%"
        };

    public VaultParticipantAllocationReadModel WithContribution(
        decimal amount, DateTime timestamp) =>
        this with
        {
            ContributionAmount = ContributionAmount + amount,
            LastUpdated = timestamp,
            AllocationSummary = $"Contribution of {amount:F2} recorded"
        };

    public VaultParticipantAllocationReadModel WithProfitShare(
        decimal profitShare, DateTime timestamp) =>
        this with
        {
            ProfitSharePercentage = profitShare,
            LastUpdated = timestamp,
            AllocationSummary = $"Profit share updated to {profitShare:F2}%"
        };
}
