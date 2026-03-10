using System.Text.Json;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Engine;
using Whycespace.Projections.Storage;

namespace Whycespace.Projections.Projections;

public sealed class VaultBalanceProjection : IProjection
{
    private readonly IProjectionStore _store;

    public VaultBalanceProjection(IProjectionStore store)
    {
        _store = store;
    }

    public string Name => "VaultBalanceProjection";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "CapitalContributionRecordedEvent",
        "ProfitDistributedEvent"
    ];

    public async Task HandleAsync(EventEnvelope envelope)
    {
        var payload = ExtractPayload(envelope.Payload);
        if (payload is null)
            return;

        var vaultId = payload.GetValueOrDefault("vaultId")?.ToString();
        if (vaultId is null)
            return;

        var amount = Convert.ToDecimal(payload.GetValueOrDefault("amount") ?? 0);

        var existing = await _store.GetAsync($"vault:{vaultId}");
        var currentBalance = existing is not null
            ? JsonSerializer.Deserialize<VaultState>(existing)?.Balance ?? 0m
            : 0m;

        var newBalance = envelope.EventType switch
        {
            "CapitalContributionRecordedEvent" => currentBalance + amount,
            "ProfitDistributedEvent" => currentBalance + amount,
            _ => currentBalance
        };

        var model = new VaultState(vaultId, newBalance);

        await _store.SetAsync($"vault:{vaultId}", JsonSerializer.Serialize(model));
    }

    private static Dictionary<string, object>? ExtractPayload(object payload)
    {
        if (payload is Dictionary<string, object> dict)
            return dict;

        if (payload is JsonElement element)
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());

        return null;
    }

    private sealed record VaultState(string VaultId, decimal Balance);
}
