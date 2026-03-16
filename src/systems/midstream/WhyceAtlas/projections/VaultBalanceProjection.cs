using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.Systems.Midstream.WhyceAtlas.Projections.Models;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections;

public sealed class VaultBalanceProjection : IProjection
{
    private readonly IProjectionStore _store;
    private readonly VaultBalanceProjectionHandler _handler = new();

    public VaultBalanceProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "VaultBalanceProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "VaultCreated",
        "VaultContributionCompleted",
        "VaultTransferCompleted",
        "VaultWithdrawalCompleted",
        "VaultProfitDistributionCompleted",
        "VaultTransactionCompleted"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var vaultId = ResolveVaultId(payload, envelope);
        if (vaultId is null)
            return;

        var storeKey = $"vault-balance:{vaultId}";
        var existing = await LoadModelAsync(storeKey);
        var timestamp = envelope.Timestamp.Value.UtcDateTime;

        var updated = envelope.EventType switch
        {
            "VaultCreated" =>
                _handler.HandleVaultCreated(existing, payload, timestamp),

            "VaultContributionCompleted" =>
                _handler.HandleContribution(
                    existing ?? VaultBalanceModel.Initial(vaultId), payload, timestamp),

            "VaultTransferCompleted" =>
                _handler.HandleTransfer(
                    existing ?? VaultBalanceModel.Initial(vaultId), payload, timestamp, vaultId),

            "VaultWithdrawalCompleted" =>
                _handler.HandleWithdrawal(
                    existing ?? VaultBalanceModel.Initial(vaultId), payload, timestamp),

            "VaultProfitDistributionCompleted" =>
                _handler.HandleProfitDistributed(
                    existing ?? VaultBalanceModel.Initial(vaultId), payload, timestamp),

            "VaultTransactionCompleted" =>
                _handler.HandleTransactionRecorded(
                    existing ?? VaultBalanceModel.Initial(vaultId), payload, timestamp),

            _ => existing
        };

        if (updated is not null)
            await _store.SetAsync(storeKey, JsonSerializer.Serialize(updated));
    }

    private async Task<VaultBalanceModel?> LoadModelAsync(string key)
    {
        var json = await _store.GetAsync(key);
        return json is not null
            ? JsonSerializer.Deserialize<VaultBalanceModel>(json)
            : null;
    }

    private static string? ResolveVaultId(Dictionary<string, object> payload, EventEnvelope envelope)
    {
        var vaultId = payload.GetValueOrDefault("vaultId")?.ToString();
        if (vaultId is not null)
            return vaultId;

        return envelope.AggregateId;
    }

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }
}
