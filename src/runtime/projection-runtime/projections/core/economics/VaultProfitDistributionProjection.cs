using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Core.Economics.Models;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Projections.Core.Economics;

public sealed class VaultProfitDistributionProjection : IProjection
{
    private readonly IProjectionStore _store;

    public VaultProfitDistributionProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "VaultProfitDistributionProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "VaultProfitDistributedEvent",
        "VaultProfitDistributionRecordedEvent",
        "VaultProfitDistributionAdjustedEvent"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var vaultId = payload.GetValueOrDefault("vaultId")?.ToString();
        if (vaultId is null)
            return;

        switch (envelope.EventType)
        {
            case "VaultProfitDistributedEvent":
                await HandleProfitDistributed(payload, vaultId);
                break;
            case "VaultProfitDistributionRecordedEvent":
                await HandleDistributionRecorded(payload, vaultId);
                break;
            case "VaultProfitDistributionAdjustedEvent":
                await HandleDistributionAdjusted(payload, vaultId);
                break;
        }
    }

    private async Task HandleProfitDistributed(Dictionary<string, object> payload, string vaultId)
    {
        var distributionId = payload.GetValueOrDefault("distributionId")?.ToString();
        var participantId = payload.GetValueOrDefault("participantId")?.ToString();
        if (distributionId is null || participantId is null)
            return;

        var amount = Convert.ToDecimal(payload.GetValueOrDefault("profitAmount") ?? 0);
        var currency = payload.GetValueOrDefault("currency")?.ToString() ?? "WHY";
        var distributionType = payload.GetValueOrDefault("distributionType")?.ToString() ?? "ParticipantProfit";
        var reference = payload.GetValueOrDefault("distributionReference")?.ToString();
        var summary = payload.GetValueOrDefault("distributionSummary")?.ToString();

        var model = new VaultProfitDistributionReadModel(
            Guid.Parse(distributionId),
            Guid.Parse(vaultId),
            Guid.Parse(participantId),
            amount,
            currency,
            distributionType,
            DateTime.UtcNow,
            DateTime.UtcNow,
            reference,
            summary);

        var key = BuildKey(vaultId, distributionId, participantId);
        await _store.SetAsync(key, JsonSerializer.Serialize(model));

        await AppendToIndex(vaultId, key);
        await AppendToParticipantIndex(vaultId, participantId, key);
    }

    private async Task HandleDistributionRecorded(Dictionary<string, object> payload, string vaultId)
    {
        var distributionId = payload.GetValueOrDefault("distributionId")?.ToString();
        var participantId = payload.GetValueOrDefault("participantId")?.ToString();
        if (distributionId is null || participantId is null)
            return;

        var key = BuildKey(vaultId, distributionId, participantId);
        var existing = await _store.GetAsync(key);
        if (existing is null)
            return;

        var model = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(existing);
        if (model is null)
            return;

        var summary = payload.GetValueOrDefault("distributionSummary")?.ToString() ?? model.DistributionSummary;

        var updated = model with
        {
            DistributionSummary = summary,
            RecordedAt = DateTime.UtcNow
        };

        await _store.SetAsync(key, JsonSerializer.Serialize(updated));
    }

    private async Task HandleDistributionAdjusted(Dictionary<string, object> payload, string vaultId)
    {
        var distributionId = payload.GetValueOrDefault("distributionId")?.ToString();
        var participantId = payload.GetValueOrDefault("participantId")?.ToString();
        if (distributionId is null || participantId is null)
            return;

        var key = BuildKey(vaultId, distributionId, participantId);
        var existing = await _store.GetAsync(key);
        if (existing is null)
            return;

        var model = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(existing);
        if (model is null)
            return;

        var adjustedAmount = Convert.ToDecimal(payload.GetValueOrDefault("profitAmount") ?? model.ProfitAmount);
        var adjustedType = payload.GetValueOrDefault("distributionType")?.ToString() ?? model.DistributionType;

        var updated = model with
        {
            ProfitAmount = adjustedAmount,
            DistributionType = adjustedType,
            RecordedAt = DateTime.UtcNow
        };

        await _store.SetAsync(key, JsonSerializer.Serialize(updated));
    }

    private async Task AppendToIndex(string vaultId, string recordKey)
    {
        var indexKey = $"vault-profit-dist-index:{vaultId}";
        var existing = await _store.GetAsync(indexKey);
        var keys = existing is not null
            ? JsonSerializer.Deserialize<List<string>>(existing) ?? []
            : [];

        if (!keys.Contains(recordKey))
        {
            keys.Add(recordKey);
            await _store.SetAsync(indexKey, JsonSerializer.Serialize(keys));
        }
    }

    private async Task AppendToParticipantIndex(string vaultId, string participantId, string recordKey)
    {
        var indexKey = $"vault-profit-dist-participant:{vaultId}:{participantId}";
        var existing = await _store.GetAsync(indexKey);
        var keys = existing is not null
            ? JsonSerializer.Deserialize<List<string>>(existing) ?? []
            : [];

        if (!keys.Contains(recordKey))
        {
            keys.Add(recordKey);
            await _store.SetAsync(indexKey, JsonSerializer.Serialize(keys));
        }
    }

    private static string BuildKey(string vaultId, string distributionId, string participantId) =>
        $"vault-profit-dist:{vaultId}:{distributionId}:{participantId}";

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }
}
