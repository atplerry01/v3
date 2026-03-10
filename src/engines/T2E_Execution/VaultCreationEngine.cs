namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;

public sealed class VaultCreationEngine : IEngine
{
    public string Name => "VaultCreation";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var ownerId = context.Data.GetValueOrDefault("ownerId") as string;
        if (string.IsNullOrEmpty(ownerId))
            return Task.FromResult(EngineResult.Fail("Missing ownerId"));

        if (!Guid.TryParse(ownerId, out var ownerGuid))
            return Task.FromResult(EngineResult.Fail("Invalid ownerId format"));

        var currency = context.Data.GetValueOrDefault("currency") as string ?? "GBP";
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        var initialBalance = ResolveDecimal(context.Data.GetValueOrDefault("initialBalance"), 0m);
        if (initialBalance < 0)
            return Task.FromResult(EngineResult.Fail("Initial balance cannot be negative"));

        var vaultId = Guid.NewGuid();

        // Event compatible with whyce.economic.events topic
        var events = new[]
        {
            EngineEvent.Create("VaultCreated", vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultId.ToString(),
                    ["ownerId"] = ownerId,
                    ["balance"] = initialBalance,
                    ["currency"] = currency,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["ownerId"] = ownerId,
                ["balance"] = initialBalance,
                ["currency"] = currency
            }));
    }

    private static decimal ResolveDecimal(object? value, decimal fallback)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => fallback
        };
    }
}
