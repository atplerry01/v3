
using System.Text.Json;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections.Vault;

public sealed class VaultParticipantAllocationProjection : IProjection
{
    private readonly IProjectionStore _store;
    private readonly VaultParticipantAllocationProjectionHandler _handler = new();

    public VaultParticipantAllocationProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "VaultParticipantAllocationProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "VaultParticipantAdded",
        "VaultParticipantRemoved",
        "VaultAllocationCreated",
        "VaultAllocationUpdated",
        "VaultContributionRecorded",
        "VaultProfitDistributed"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var vaultId = payload.GetValueOrDefault("vaultId")?.ToString();
        var participantId = payload.GetValueOrDefault("participantId")?.ToString();
        if (vaultId is null || participantId is null)
            return;

        var key = $"vault-allocation:{vaultId}:{participantId}";
        var timestamp = envelope.Timestamp.Value.UtcDateTime;

        if (envelope.EventType == "VaultParticipantRemoved")
        {
            await _store.DeleteAsync(key);
            return;
        }

        var existing = await LoadModelAsync(key);

        var updated = envelope.EventType switch
        {
            "VaultParticipantAdded" => _handler.HandleParticipantAdded(existing, payload, timestamp),
            "VaultAllocationCreated" => _handler.HandleAllocationCreated(
                existing ?? CreateDefault(vaultId, participantId, timestamp), payload, timestamp),
            "VaultAllocationUpdated" => _handler.HandleAllocationUpdated(
                existing ?? CreateDefault(vaultId, participantId, timestamp), payload, timestamp),
            "VaultContributionRecorded" => _handler.HandleContributionRecorded(
                existing ?? CreateDefault(vaultId, participantId, timestamp), payload, timestamp),
            "VaultProfitDistributed" => _handler.HandleProfitDistributed(
                existing ?? CreateDefault(vaultId, participantId, timestamp), payload, timestamp),
            _ => existing
        };

        if (updated is not null)
            await _store.SetAsync(key, JsonSerializer.Serialize(updated));
    }

    private async Task<VaultParticipantAllocationReadModel?> LoadModelAsync(string key)
    {
        var json = await _store.GetAsync(key);
        return json is not null
            ? JsonSerializer.Deserialize<VaultParticipantAllocationReadModel>(json)
            : null;
    }

    private static VaultParticipantAllocationReadModel CreateDefault(
        string vaultId, string participantId, DateTime timestamp) =>
        VaultParticipantAllocationReadModel.Initial(
            Guid.Empty, Guid.Parse(vaultId), Guid.Parse(participantId), timestamp);

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }
}
