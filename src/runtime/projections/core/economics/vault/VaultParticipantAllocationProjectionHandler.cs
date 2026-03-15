using System.Text.Json;

namespace Whycespace.Projections.Core.Economics.Vault;

public sealed class VaultParticipantAllocationProjectionHandler
{
    public VaultParticipantAllocationReadModel HandleParticipantAdded(
        VaultParticipantAllocationReadModel? existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var allocationId = ExtractGuid(payload, "allocationId");
        var vaultId = ExtractGuid(payload, "vaultId");
        var participantId = ExtractGuid(payload, "participantId");
        var role = payload.GetValueOrDefault("participantRole")?.ToString();

        var model = existing
            ?? VaultParticipantAllocationReadModel.Initial(allocationId, vaultId, participantId, timestamp);

        return model with { ParticipantRole = role, LastUpdated = timestamp };
    }

    public VaultParticipantAllocationReadModel HandleAllocationCreated(
        VaultParticipantAllocationReadModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var percentage = ExtractDecimal(payload, "allocationPercentage");
        var profitShare = ExtractDecimal(payload, "profitSharePercentage");

        return existing.WithAllocation(percentage, profitShare, timestamp);
    }

    public VaultParticipantAllocationReadModel HandleAllocationUpdated(
        VaultParticipantAllocationReadModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var percentage = ExtractDecimal(payload, "allocationPercentage");
        var profitShare = ExtractDecimal(payload, "profitSharePercentage");

        return existing.WithAllocation(percentage, profitShare, timestamp);
    }

    public VaultParticipantAllocationReadModel HandleContributionRecorded(
        VaultParticipantAllocationReadModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var amount = ExtractDecimal(payload, "amount");
        return existing.WithContribution(amount, timestamp);
    }

    public VaultParticipantAllocationReadModel HandleProfitDistributed(
        VaultParticipantAllocationReadModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var profitShare = ExtractDecimal(payload, "profitSharePercentage");
        return existing.WithProfitShare(profitShare, timestamp);
    }

    private static decimal ExtractDecimal(Dictionary<string, object> payload, string key)
    {
        var value = payload.GetValueOrDefault(key);
        if (value is null) return 0m;
        if (value is JsonElement element) return element.GetDecimal();
        return Convert.ToDecimal(value);
    }

    private static Guid ExtractGuid(Dictionary<string, object> payload, string key)
    {
        var value = payload.GetValueOrDefault(key)?.ToString();
        return Guid.TryParse(value, out var guid) ? guid : Guid.Empty;
    }
}
