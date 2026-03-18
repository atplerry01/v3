namespace Whycespace.Engines.T2E.Economic.Vault.Governance.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultGovernanceRule", EngineTier.T2E, EngineKind.Validation, "ValidateVaultGovernanceCommand", typeof(EngineEvent))]
public sealed class VaultGovernanceRuleEngine : IEngine
{
    public string Name => "VaultGovernanceRule";

    private static readonly string[] ValidOperationTypes =
    {
        "Withdrawal", "Transfer", "ProfitDistribution", "Contribution",
        "TreasuryWithdrawal", "Allocation", "Disbursement"
    };

    private static readonly string[] ValidGovernanceScopes =
    {
        "GeneralPurpose", "InvestmentCapital", "SPVCapital", "RevenueCollection",
        "ProfitDistribution", "OperationalTreasury", "Escrow", "InfrastructureFunding", "GrantFunding"
    };

    // Threshold governance: operations above these amounts require governance approval
    private const decimal StandardApprovalThreshold = 100_000m;
    private const decimal MultiPartyApprovalThreshold = 250_000m;
    private const decimal GuardianOversightThreshold = 1_000_000m;

    // Multi-party approval quorum
    private const int MultiPartyQuorumRequired = 2;
    private const int MultiPartyQuorumTotal = 3;

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Input Validation ---

        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr) || !Guid.TryParse(vaultIdStr, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultId"));

        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr) || !Guid.TryParse(vaultAccountIdStr, out var vaultAccountId) || vaultAccountId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultAccountId"));

        var initiatorIdStr = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdStr) || !Guid.TryParse(initiatorIdStr, out var initiatorIdentityId) || initiatorIdentityId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid initiatorIdentityId"));

        var operationType = context.Data.GetValueOrDefault("operationType") as string;
        if (string.IsNullOrWhiteSpace(operationType))
            return Task.FromResult(EngineResult.Fail("Missing operationType"));
        if (!Array.Exists(ValidOperationTypes, t => t == operationType))
            return Task.FromResult(EngineResult.Fail($"Invalid operationType: {operationType}. Valid: {string.Join(", ", ValidOperationTypes)}"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null || amount < 0)
            return Task.FromResult(EngineResult.Fail("Missing or invalid amount"));

        var governanceScope = context.Data.GetValueOrDefault("governanceScope") as string;
        if (string.IsNullOrWhiteSpace(governanceScope))
            return Task.FromResult(EngineResult.Fail("Missing governanceScope"));
        if (!Array.Exists(ValidGovernanceScopes, s => s == governanceScope))
            return Task.FromResult(EngineResult.Fail($"Invalid governanceScope: {governanceScope}. Valid: {string.Join(", ", ValidGovernanceScopes)}"));

        var requestedAtStr = context.Data.GetValueOrDefault("requestedAt") as string;
        var requestedAt = DateTime.TryParse(requestedAtStr, out var parsedDate) ? parsedDate : DateTime.UtcNow;

        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        var approvalCount = ResolveInt(context.Data.GetValueOrDefault("approvalCount"));

        // --- Governance Rule Evaluation ---

        var evaluatedAt = DateTime.UtcNow;
        var amountValue = amount.Value;

        // Rule 1: Governance scope restrictions
        var scopeRestriction = EvaluateScopeRestriction(governanceScope, operationType);
        if (scopeRestriction is not null)
            return BuildResult(vaultId, operationType, false, "Rejected", scopeRestriction, evaluatedAt, referenceId, referenceType);

        // Rule 2: Guardian oversight (highest threshold)
        if (amountValue > GuardianOversightThreshold)
        {
            var hasGuardianQuorum = context.Data.GetValueOrDefault("guardianQuorumSatisfied") as string == "true";
            if (!hasGuardianQuorum)
                return BuildResult(vaultId, operationType, false, "GuardianOversightRequired",
                    $"Operations exceeding {GuardianOversightThreshold:N0} require Guardian quorum approval", evaluatedAt, referenceId, referenceType);
        }

        // Rule 3: Multi-party approval
        if (amountValue > MultiPartyApprovalThreshold)
        {
            var currentApprovals = approvalCount ?? 0;
            if (currentApprovals < MultiPartyQuorumRequired)
                return BuildResult(vaultId, operationType, false, "MultiPartyApprovalRequired",
                    $"Operations exceeding {MultiPartyApprovalThreshold:N0} require {MultiPartyQuorumRequired}-of-{MultiPartyQuorumTotal} approvals (current: {currentApprovals})",
                    evaluatedAt, referenceId, referenceType);
        }

        // Rule 4: Standard threshold approval
        if (amountValue > StandardApprovalThreshold)
        {
            var hasGovernanceApproval = context.Data.GetValueOrDefault("governanceApproval") as string == "true";
            if (!hasGovernanceApproval)
                return BuildResult(vaultId, operationType, false, "GovernanceApprovalRequired",
                    $"Operations exceeding {StandardApprovalThreshold:N0} require governance approval", evaluatedAt, referenceId, referenceType);
        }

        // All governance rules satisfied
        return BuildResult(vaultId, operationType, true, "Approved", "All governance conditions satisfied", evaluatedAt, referenceId, referenceType);
    }

    private static string? EvaluateScopeRestriction(string governanceScope, string operationType)
    {
        return governanceScope switch
        {
            "InfrastructureFunding" when operationType is "ProfitDistribution" or "Disbursement" =>
                "InfrastructureFunding vaults restrict profit distributions and disbursements",
            "Escrow" when operationType is "ProfitDistribution" =>
                "Escrow vaults do not permit profit distribution operations",
            "GrantFunding" when operationType is "ProfitDistribution" or "Transfer" =>
                "GrantFunding vaults restrict profit distributions and transfers",
            _ => null
        };
    }

    private static Task<EngineResult> BuildResult(
        Guid vaultId, string operationType, bool isApproved,
        string decision, string reason, DateTime evaluatedAt,
        string? referenceId, string? referenceType)
    {
        var eventType = isApproved ? "VaultGovernanceValidationApproved" : "VaultGovernanceValidationRejected";

        var eventPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["operationType"] = operationType,
            ["isApproved"] = isApproved,
            ["governanceDecision"] = decision,
            ["governanceReason"] = reason,
            ["evaluatedAt"] = evaluatedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        if (referenceId is not null) eventPayload["referenceId"] = referenceId;
        if (referenceType is not null) eventPayload["referenceType"] = referenceType;

        var requestedEvent = EngineEvent.Create("VaultGovernanceValidationRequested", vaultId,
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["operationType"] = operationType,
                ["evaluatedAt"] = evaluatedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            });

        var resultEvent = EngineEvent.Create(eventType, vaultId, eventPayload);

        var events = new[] { requestedEvent, resultEvent };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["operationType"] = operationType,
            ["isApproved"] = isApproved,
            ["governanceDecision"] = decision,
            ["governanceReason"] = reason,
            ["evaluatedAt"] = evaluatedAt.ToString("O")
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

    private static int? ResolveInt(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
