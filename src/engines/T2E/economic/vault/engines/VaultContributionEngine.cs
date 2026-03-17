namespace Whycespace.Engines.T2E.Economic.Vault.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultContribution", EngineTier.T2E, EngineKind.Mutation, "VaultContributionRequest", typeof(EngineEvent))]
public sealed class VaultContributionEngine : IEngine
{
    public string Name => "VaultContribution";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };
    private static readonly string[] ValidContributionSources = { "Investor", "Treasury", "Revenue", "Grant" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // Resolve contribution ID
        var contributionIdStr = context.Data.GetValueOrDefault("contributionId") as string;
        if (string.IsNullOrEmpty(contributionIdStr))
            return Task.FromResult(EngineResult.Fail("Missing contributionId"));
        if (!Guid.TryParse(contributionIdStr, out var contributionId))
            return Task.FromResult(EngineResult.Fail("Invalid contributionId format"));

        // Resolve vault ID
        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdStr, out var vaultId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // Resolve vault account ID
        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdStr, out var vaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // Resolve contributor identity ID
        var contributorIdentityIdStr = context.Data.GetValueOrDefault("contributorIdentityId") as string;
        if (string.IsNullOrEmpty(contributorIdentityIdStr))
            return Task.FromResult(EngineResult.Fail("Missing contributorIdentityId"));
        if (!Guid.TryParse(contributorIdentityIdStr, out var contributorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid contributorIdentityId format"));

        // Resolve amount
        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null or <= 0)
            return Task.FromResult(EngineResult.Fail("Amount must be greater than zero"));

        // Resolve currency
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // Resolve contribution source
        var contributionSource = context.Data.GetValueOrDefault("contributionSource") as string;
        if (string.IsNullOrEmpty(contributionSource))
            return Task.FromResult(EngineResult.Fail("Missing contributionSource"));
        if (!Array.Exists(ValidContributionSources, s => s == contributionSource))
            return Task.FromResult(EngineResult.Fail($"Invalid contributionSource: {contributionSource}. Valid: {string.Join(", ", ValidContributionSources)}"));

        // Optional fields
        var description = context.Data.GetValueOrDefault("description") as string ?? "";
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        var transactionId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow;

        // Event 1: VaultContributionReceived — contribution accepted and processing started
        var receivedEvent = EngineEvent.Create("VaultContributionReceived", vaultId,
            new Dictionary<string, object>
            {
                ["contributionId"] = contributionId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = vaultAccountId.ToString(),
                ["contributorIdentityId"] = contributorIdentityId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["contributionSource"] = contributionSource,
                ["description"] = description,
                ["topic"] = "whyce.economic.events"
            });

        // Event 2: VaultContributionProcessed — ledger credit entry appended
        var processedEvent = EngineEvent.Create("VaultContributionProcessed", vaultId,
            new Dictionary<string, object>
            {
                ["contributionId"] = contributionId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = vaultAccountId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["ledgerDirection"] = "Credit",
                ["ledgerTransactionType"] = "Contribution",
                ["topic"] = "whyce.economic.events"
            });

        // Event 3: VaultContributionCompleted — transaction registered and finalized
        var completedEventPayload = new Dictionary<string, object>
        {
            ["contributionId"] = contributionId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["contributorIdentityId"] = contributorIdentityId.ToString(),
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["transactionStatus"] = "Completed",
            ["completedAt"] = completedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        if (!string.IsNullOrEmpty(referenceId))
            completedEventPayload["referenceId"] = referenceId;
        if (!string.IsNullOrEmpty(referenceType))
            completedEventPayload["referenceType"] = referenceType;

        var completedEvent = EngineEvent.Create("VaultContributionCompleted", vaultId, completedEventPayload);

        var events = new[] { receivedEvent, processedEvent, completedEvent };

        var output = new Dictionary<string, object>
        {
            ["contributionId"] = contributionId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["vaultAccountId"] = vaultAccountId.ToString(),
            ["contributorIdentityId"] = contributorIdentityId.ToString(),
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["contributionSource"] = contributionSource,
            ["transactionStatus"] = "Completed",
            ["completedAt"] = completedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
