namespace Whycespace.Engines.T2E.Economic.Vault.Creation.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultCreation", EngineTier.T2E, EngineKind.Mutation, "CreateVaultCommand", typeof(EngineEvent))]
public sealed class VaultCreationEngine : IEngine
{
    public string Name => "VaultCreation";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    private static readonly string[] ValidPurposes =
    {
        "GeneralPurpose", "InvestmentCapital", "SPVCapital", "RevenueCollection",
        "ProfitDistribution", "OperationalTreasury", "Escrow", "InfrastructureFunding", "GrantFunding"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // Validate VaultName
        var vaultName = context.Data.GetValueOrDefault("vaultName") as string;
        if (string.IsNullOrWhiteSpace(vaultName))
            return Task.FromResult(EngineResult.Fail("Missing or empty vaultName"));

        // Validate OwnerIdentityId
        var ownerId = context.Data.GetValueOrDefault("ownerId") as string;
        if (string.IsNullOrEmpty(ownerId))
            return Task.FromResult(EngineResult.Fail("Missing ownerId"));

        if (!Guid.TryParse(ownerId, out var ownerGuid) || ownerGuid == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid ownerId format"));

        // Validate Currency
        var currency = context.Data.GetValueOrDefault("currency") as string ?? "GBP";
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // Validate VaultPurpose
        var vaultPurpose = context.Data.GetValueOrDefault("vaultPurpose") as string ?? "GeneralPurpose";
        if (!Array.Exists(ValidPurposes, p => p == vaultPurpose))
            return Task.FromResult(EngineResult.Fail($"Invalid vault purpose: {vaultPurpose}. Valid: {string.Join(", ", ValidPurposes)}"));

        // Optional fields
        var description = context.Data.GetValueOrDefault("description") as string ?? string.Empty;
        var cluster = context.Data.GetValueOrDefault("cluster") as string;
        var subCluster = context.Data.GetValueOrDefault("subCluster") as string;
        var spv = context.Data.GetValueOrDefault("spv") as string;

        var initialBalance = ResolveDecimal(context.Data.GetValueOrDefault("initialBalance"), 0m);
        if (initialBalance < 0)
            return Task.FromResult(EngineResult.Fail("Initial balance cannot be negative"));

        // Generate identifiers
        var vaultId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var policyStateId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Build VaultCreated event payload
        var eventPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["vaultName"] = vaultName,
            ["ownerId"] = ownerId,
            ["vaultPurpose"] = vaultPurpose,
            ["currency"] = currency,
            ["initialBalance"] = initialBalance,
            ["description"] = description,
            ["accountId"] = accountId.ToString(),
            ["policyStateId"] = policyStateId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["vaultStatus"] = "Active",
            ["policyStatus"] = "Compliant",
            ["riskLevel"] = "Low",
            ["createdAt"] = createdAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        if (cluster is not null) eventPayload["cluster"] = cluster;
        if (subCluster is not null) eventPayload["subCluster"] = subCluster;
        if (spv is not null) eventPayload["spv"] = spv;

        var events = new[]
        {
            EngineEvent.Create("VaultCreated", vaultId, eventPayload)
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["vaultName"] = vaultName,
            ["ownerId"] = ownerId,
            ["vaultPurpose"] = vaultPurpose,
            ["vaultStatus"] = "Active",
            ["currency"] = currency,
            ["balance"] = initialBalance,
            ["accountId"] = accountId.ToString(),
            ["policyStateId"] = policyStateId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["createdAt"] = createdAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
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
