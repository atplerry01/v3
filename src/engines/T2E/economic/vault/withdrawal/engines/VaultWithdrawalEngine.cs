namespace Whycespace.Engines.T2E.Economic.Vault.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultWithdrawal", EngineTier.T2E, EngineKind.Mutation, "ExecuteVaultWithdrawalCommand", typeof(EngineEvent))]
public sealed class VaultWithdrawalEngine : IEngine
{
    public string Name => "VaultWithdrawal";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    private static readonly string[] ValidWithdrawalDestinations =
    {
        "ExternalBank", "ParticipantPayout", "OperationalExpense", "TreasuryTransfer"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate WithdrawalId ---
        var withdrawalIdStr = context.Data.GetValueOrDefault("withdrawalId") as string;
        if (string.IsNullOrEmpty(withdrawalIdStr))
            return Task.FromResult(EngineResult.Fail("Missing withdrawalId"));
        if (!Guid.TryParse(withdrawalIdStr, out var withdrawalId))
            return Task.FromResult(EngineResult.Fail("Invalid withdrawalId format"));

        // --- Validate VaultId ---
        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdStr, out var vaultId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdStr, out var vaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdStr = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdStr))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdStr, out var initiatorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

        // --- Validate Amount ---
        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Amount must be greater than zero"));

        // --- Validate Currency ---
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // --- Validate WithdrawalDestination ---
        var withdrawalDestination = context.Data.GetValueOrDefault("withdrawalDestination") as string;
        if (string.IsNullOrEmpty(withdrawalDestination))
            return Task.FromResult(EngineResult.Fail("Missing withdrawalDestination"));
        if (!Array.Exists(ValidWithdrawalDestinations, d => d == withdrawalDestination))
            return Task.FromResult(EngineResult.Fail($"Invalid withdrawalDestination: {withdrawalDestination}. Valid: {string.Join(", ", ValidWithdrawalDestinations)}"));

        // --- Validate Sufficient Funds ---
        var availableBalance = ResolveDecimal(context.Data.GetValueOrDefault("availableBalance"));
        if (availableBalance is not null && amount.Value > availableBalance.Value)
            return Task.FromResult(EngineResult.Fail("Insufficient funds for withdrawal"));

        // --- Optional fields ---
        var description = context.Data.GetValueOrDefault("description") as string ?? "";
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        var transactionId = Guid.NewGuid();
        var completedAt = DateTimeOffset.UtcNow;

        // Event 1: VaultWithdrawalRequested — withdrawal accepted and validation passed
        var requestedEvent = EngineEvent.Create("VaultWithdrawalRequested", vaultId,
            new Dictionary<string, object>
            {
                ["withdrawalId"] = withdrawalId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = vaultAccountId.ToString(),
                ["initiatorIdentityId"] = initiatorIdentityId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["withdrawalDestination"] = withdrawalDestination,
                ["description"] = description,
                ["topic"] = "whyce.economic.events"
            });

        // Event 2: VaultWithdrawalProcessing — debit ledger entry appended
        var processingEvent = EngineEvent.Create("VaultWithdrawalProcessing", vaultId,
            new Dictionary<string, object>
            {
                ["withdrawalId"] = withdrawalId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = vaultAccountId.ToString(),
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["ledgerDirection"] = "Debit",
                ["ledgerTransactionType"] = "Withdrawal",
                ["topic"] = "whyce.economic.events"
            });

        // Event 3: VaultWithdrawalCompleted — transaction registered and finalized
        var completedEventPayload = new Dictionary<string, object>
        {
            ["withdrawalId"] = withdrawalId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["vaultAccountId"] = vaultAccountId.ToString(),
            ["initiatorIdentityId"] = initiatorIdentityId.ToString(),
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["withdrawalDestination"] = withdrawalDestination,
            ["transactionStatus"] = "Completed",
            ["completedAt"] = completedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        if (!string.IsNullOrEmpty(referenceId))
            completedEventPayload["referenceId"] = referenceId;
        if (!string.IsNullOrEmpty(referenceType))
            completedEventPayload["referenceType"] = referenceType;

        var completedEvent = EngineEvent.Create("VaultWithdrawalCompleted", vaultId, completedEventPayload);

        var events = new[] { requestedEvent, processingEvent, completedEvent };

        var output = new Dictionary<string, object>
        {
            ["withdrawalId"] = withdrawalId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["vaultAccountId"] = vaultAccountId.ToString(),
            ["initiatorIdentityId"] = initiatorIdentityId.ToString(),
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["withdrawalDestination"] = withdrawalDestination,
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