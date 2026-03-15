using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Contracts;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Projections.Core.Economics.Vault;

public sealed class VaultTransactionProjection : IProjection
{
    private readonly IProjectionStore _store;

    public VaultTransactionProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "VaultTransactionProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "VaultTransactionCreated",
        "VaultContributionRecorded",
        "VaultTransferExecuted",
        "VaultWithdrawalExecuted",
        "VaultProfitDistributed",
        "VaultTransactionCompleted"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var record = VaultTransactionProjectionHandler.Handle(envelope.EventType, payload);
        if (record is null)
            return;

        // Idempotency: use transaction ID as part of the key
        var txKey = $"vault-tx:{record.VaultId}:{record.TransactionId}";
        var existing = await _store.GetAsync(txKey);
        if (existing is not null)
            return;

        await _store.SetAsync(txKey, JsonSerializer.Serialize(record));

        // Maintain ordered transaction index per vault
        var indexKey = $"vault-tx-index:{record.VaultId}";
        var indexJson = await _store.GetAsync(indexKey);
        var index = indexJson is not null
            ? JsonSerializer.Deserialize<List<VaultTransactionIndexEntry>>(indexJson) ?? []
            : [];

        index.Add(new VaultTransactionIndexEntry(
            record.TransactionId,
            record.TransactionTimestamp));

        index.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        await _store.SetAsync(indexKey, JsonSerializer.Serialize(index));
    }

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }

    private sealed record VaultTransactionIndexEntry(Guid TransactionId, DateTime Timestamp);
}
