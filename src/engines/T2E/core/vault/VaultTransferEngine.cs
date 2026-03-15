namespace Whycespace.Engines.T2E.Core.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultTransfer", EngineTier.T2E, EngineKind.Mutation, "VaultTransferRequest", typeof(EngineEvent))]
public sealed class VaultTransferEngine : IEngine
{
    public string Name => "VaultTransfer";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // Resolve transfer ID
        var transferIdStr = context.Data.GetValueOrDefault("transferId") as string;
        if (string.IsNullOrEmpty(transferIdStr))
            return Task.FromResult(EngineResult.Fail("Missing transferId"));
        if (!Guid.TryParse(transferIdStr, out var transferId))
            return Task.FromResult(EngineResult.Fail("Invalid transferId format"));

        // Resolve source vault ID
        var sourceVaultIdStr = context.Data.GetValueOrDefault("sourceVaultId") as string;
        if (string.IsNullOrEmpty(sourceVaultIdStr))
            return Task.FromResult(EngineResult.Fail("Missing sourceVaultId"));
        if (!Guid.TryParse(sourceVaultIdStr, out var sourceVaultId))
            return Task.FromResult(EngineResult.Fail("Invalid sourceVaultId format"));

        // Resolve source vault account ID
        var sourceVaultAccountIdStr = context.Data.GetValueOrDefault("sourceVaultAccountId") as string;
        if (string.IsNullOrEmpty(sourceVaultAccountIdStr))
            return Task.FromResult(EngineResult.Fail("Missing sourceVaultAccountId"));
        if (!Guid.TryParse(sourceVaultAccountIdStr, out var sourceVaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid sourceVaultAccountId format"));

        // Resolve destination vault ID
        var destinationVaultIdStr = context.Data.GetValueOrDefault("destinationVaultId") as string;
        if (string.IsNullOrEmpty(destinationVaultIdStr))
            return Task.FromResult(EngineResult.Fail("Missing destinationVaultId"));
        if (!Guid.TryParse(destinationVaultIdStr, out var destinationVaultId))
            return Task.FromResult(EngineResult.Fail("Invalid destinationVaultId format"));

        // Resolve destination vault account ID
        var destinationVaultAccountIdStr = context.Data.GetValueOrDefault("destinationVaultAccountId") as string;
        if (string.IsNullOrEmpty(destinationVaultAccountIdStr))
            return Task.FromResult(EngineResult.Fail("Missing destinationVaultAccountId"));
        if (!Guid.TryParse(destinationVaultAccountIdStr, out var destinationVaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid destinationVaultAccountId format"));

        // Resolve initiator identity ID
        var initiatorIdentityIdStr = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdentityIdStr))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdentityIdStr, out var initiatorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

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

        // Resolve source balance for sufficient funds check
        var sourceBalance = ResolveDecimal(context.Data.GetValueOrDefault("sourceBalance"));
        if (sourceBalance is not null && sourceBalance.Value < amount.Value)
            return Task.FromResult(EngineResult.Fail("Insufficient funds in source account"));

        // Optional fields
        var description = context.Data.GetValueOrDefault("description") as string ?? "";
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        // Validate source and destination are not the same account
        if (sourceVaultId == destinationVaultId && sourceVaultAccountId == destinationVaultAccountId)
            return Task.FromResult(EngineResult.Fail("Source and destination accounts must be different"));

        var transactionId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow;

        // Event 1: VaultTransferInitiated — transfer accepted and authorized
        var initiatedEvent = EngineEvent.Create("VaultTransferInitiated", transferId,
            new Dictionary<string, object>
            {
                ["transferId"] = transferId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["sourceVaultId"] = sourceVaultId.ToString(),
                ["sourceVaultAccountId"] = sourceVaultAccountId.ToString(),
                ["destinationVaultId"] = destinationVaultId.ToString(),
                ["destinationVaultAccountId"] = destinationVaultAccountId.ToString(),
                ["initiatorIdentityId"] = initiatorIdentityId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["description"] = description,
                ["topic"] = "whyce.economic.events"
            });

        // Event 2: VaultTransferProcessing — debit ledger entry appended (source account)
        var debitEvent = EngineEvent.Create("VaultTransferProcessing", sourceVaultId,
            new Dictionary<string, object>
            {
                ["transferId"] = transferId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = sourceVaultId.ToString(),
                ["vaultAccountId"] = sourceVaultAccountId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["ledgerDirection"] = "Debit",
                ["ledgerTransactionType"] = "TransferOut",
                ["topic"] = "whyce.economic.events"
            });

        // Event 3: VaultTransferProcessing — credit ledger entry appended (destination account)
        var creditEvent = EngineEvent.Create("VaultTransferProcessing", destinationVaultId,
            new Dictionary<string, object>
            {
                ["transferId"] = transferId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = destinationVaultId.ToString(),
                ["vaultAccountId"] = destinationVaultAccountId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["ledgerDirection"] = "Credit",
                ["ledgerTransactionType"] = "TransferIn",
                ["topic"] = "whyce.economic.events"
            });

        // Event 4: VaultTransferCompleted — transaction registered and finalized
        var completedEventPayload = new Dictionary<string, object>
        {
            ["transferId"] = transferId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["sourceVaultId"] = sourceVaultId.ToString(),
            ["destinationVaultId"] = destinationVaultId.ToString(),
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

        var completedEvent = EngineEvent.Create("VaultTransferCompleted", transferId, completedEventPayload);

        var events = new[] { initiatedEvent, debitEvent, creditEvent, completedEvent };

        var output = new Dictionary<string, object>
        {
            ["transferId"] = transferId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["sourceVaultId"] = sourceVaultId.ToString(),
            ["destinationVaultId"] = destinationVaultId.ToString(),
            ["amount"] = amount.Value,
            ["currency"] = currency,
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