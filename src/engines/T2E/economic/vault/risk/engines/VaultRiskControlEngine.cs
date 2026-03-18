namespace Whycespace.Engines.T2E.Economic.Vault.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultRiskControl", EngineTier.T2E, EngineKind.Decision, "EvaluateVaultRiskCommand", typeof(EngineEvent))]
public sealed class VaultRiskControlEngine : IEngine
{
    public string Name => "VaultRiskControl";

    private static readonly string[] ValidOperationTypes =
    {
        "Withdrawal", "Transfer", "ProfitDistribution", "Contribution",
        "TreasuryWithdrawal", "Allocation", "Disbursement", "Settlement"
    };

    private static readonly string[] ValidCurrencies = { "GBP", "USD", "EUR", "NGN" };

    // Risk score thresholds
    private const decimal LowRiskCeiling = 30m;
    private const decimal MediumRiskCeiling = 70m;

    // Risk factor weights
    private const decimal BaseTransactionSizeWeight = 40m;
    private const decimal BalanceRatioWeight = 30m;
    private const decimal FrequencyWeight = 20m;
    private const decimal BehaviorWeight = 10m;

    // Transaction size thresholds for scoring
    private const decimal SmallTransactionLimit = 10_000m;
    private const decimal MediumTransactionLimit = 50_000m;
    private const decimal LargeTransactionLimit = 250_000m;

    // Balance ratio thresholds
    private const decimal ElevatedBalanceRatio = 0.40m;
    private const decimal HighBalanceRatio = 0.50m;

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Input Validation ---

        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr) || !Guid.TryParse(vaultIdStr, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultId"));

        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr) || !Guid.TryParse(vaultAccountIdStr, out var vaultAccountId) || vaultAccountId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultAccountId"));

        var transactionIdStr = context.Data.GetValueOrDefault("transactionId") as string;
        if (string.IsNullOrEmpty(transactionIdStr) || !Guid.TryParse(transactionIdStr, out var transactionId) || transactionId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid transactionId"));

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

        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrWhiteSpace(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(ValidCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Invalid currency: {currency}. Valid: {string.Join(", ", ValidCurrencies)}"));

        var requestedAtStr = context.Data.GetValueOrDefault("requestedAt") as string;
        var requestedAt = DateTime.TryParse(requestedAtStr, out var parsedDate) ? parsedDate : DateTime.UtcNow;

        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        // --- Optional context for risk factors ---
        var vaultBalance = ResolveDecimal(context.Data.GetValueOrDefault("vaultBalance"));
        var recentTransactionCount = ResolveInt(context.Data.GetValueOrDefault("recentTransactionCount"));
        var behaviorFlag = context.Data.GetValueOrDefault("behaviorFlag") as string;

        // --- Risk Score Calculation ---
        var amountValue = amount.Value;
        var riskScore = CalculateRiskScore(amountValue, operationType, vaultBalance, recentTransactionCount, behaviorFlag);

        // --- Determine Risk Level ---
        var riskLevel = riskScore switch
        {
            <= LowRiskCeiling => "Low",
            <= MediumRiskCeiling => "Medium",
            _ => "High"
        };

        // --- Apply Risk Decision Rules ---
        var (isAllowed, riskDecision, riskReason) = ApplyRiskDecision(riskLevel, riskScore, amountValue, operationType, vaultBalance);

        var evaluatedAt = DateTime.UtcNow;

        return BuildResult(vaultId, transactionId, riskScore, riskLevel, isAllowed, riskDecision, riskReason,
            evaluatedAt, operationType, amountValue, referenceId, referenceType);
    }

    private static decimal CalculateRiskScore(decimal amount, string operationType, decimal? vaultBalance,
        int? recentTransactionCount, string? behaviorFlag)
    {
        var score = 0m;

        // Factor 1: Transaction size score (0-40)
        score += CalculateTransactionSizeScore(amount);

        // Factor 2: Balance ratio score (0-30)
        score += CalculateBalanceRatioScore(amount, vaultBalance);

        // Factor 3: Transaction frequency score (0-20)
        score += CalculateFrequencyScore(recentTransactionCount);

        // Factor 4: Behavior flag score (0-10)
        score += CalculateBehaviorScore(behaviorFlag);

        // Operation type modifier
        score += GetOperationTypeModifier(operationType);

        return Math.Clamp(Math.Round(score, 2), 0m, 100m);
    }

    private static decimal CalculateTransactionSizeScore(decimal amount)
    {
        if (amount <= SmallTransactionLimit) return BaseTransactionSizeWeight * 0.1m;
        if (amount <= MediumTransactionLimit) return BaseTransactionSizeWeight * 0.4m;
        if (amount <= LargeTransactionLimit) return BaseTransactionSizeWeight * 0.7m;
        return BaseTransactionSizeWeight;
    }

    private static decimal CalculateBalanceRatioScore(decimal amount, decimal? vaultBalance)
    {
        if (vaultBalance is null or 0) return BalanceRatioWeight * 0.5m;

        var ratio = amount / vaultBalance.Value;
        if (ratio > HighBalanceRatio) return BalanceRatioWeight;
        if (ratio > ElevatedBalanceRatio) return BalanceRatioWeight * 0.7m;
        if (ratio > 0.20m) return BalanceRatioWeight * 0.3m;
        return BalanceRatioWeight * 0.1m;
    }

    private static decimal CalculateFrequencyScore(int? recentTransactionCount)
    {
        if (recentTransactionCount is null) return 0m;
        if (recentTransactionCount > 50) return FrequencyWeight;
        if (recentTransactionCount > 20) return FrequencyWeight * 0.6m;
        if (recentTransactionCount > 10) return FrequencyWeight * 0.3m;
        return 0m;
    }

    private static decimal CalculateBehaviorScore(string? behaviorFlag)
    {
        return behaviorFlag switch
        {
            "suspicious" => BehaviorWeight,
            "unusual" => BehaviorWeight * 0.6m,
            "elevated" => BehaviorWeight * 0.3m,
            _ => 0m
        };
    }

    private static decimal GetOperationTypeModifier(string operationType)
    {
        return operationType switch
        {
            "TreasuryWithdrawal" => 5m,
            "Withdrawal" => 3m,
            "Transfer" => 2m,
            _ => 0m
        };
    }

    private static (bool IsAllowed, string Decision, string Reason) ApplyRiskDecision(
        string riskLevel, decimal riskScore, decimal amount, string operationType, decimal? vaultBalance)
    {
        switch (riskLevel)
        {
            case "Low":
                return (true, "Approved", "Transaction within acceptable risk parameters");

            case "Medium":
                return (true, "ApprovedWithMonitoring",
                    $"Transaction flagged for monitoring (risk score: {riskScore})");

            case "High":
                // Check if withdrawal exceeds 50% of vault balance
                if (vaultBalance is not null and > 0 && operationType is "Withdrawal" or "TreasuryWithdrawal"
                    && amount > vaultBalance.Value * HighBalanceRatio)
                {
                    return (false, "Blocked",
                        $"Withdrawal of {amount} exceeds 50% of vault balance ({vaultBalance.Value}). Risk score: {riskScore}");
                }

                return (false, "BlockedPendingReview",
                    $"Transaction blocked pending governance review (risk score: {riskScore})");
        }

        return (false, "Blocked", "Unable to determine risk level");
    }

    private static Task<EngineResult> BuildResult(
        Guid vaultId, Guid transactionId, decimal riskScore, string riskLevel,
        bool isAllowed, string riskDecision, string riskReason, DateTime evaluatedAt,
        string operationType, decimal amount, string? referenceId, string? referenceType)
    {
        var requestedEvent = EngineEvent.Create("VaultRiskEvaluationRequested", vaultId,
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["operationType"] = operationType,
                ["amount"] = amount,
                ["evaluatedAt"] = evaluatedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            });

        string resultEventType;
        if (isAllowed && riskLevel == "Low")
            resultEventType = "VaultRiskEvaluationPassed";
        else if (isAllowed && riskLevel == "Medium")
            resultEventType = "VaultRiskEvaluationFlagged";
        else
            resultEventType = "VaultRiskEvaluationBlocked";

        var resultPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["riskScore"] = riskScore,
            ["riskLevel"] = riskLevel,
            ["isAllowed"] = isAllowed,
            ["riskDecision"] = riskDecision,
            ["riskReason"] = riskReason,
            ["evaluatedAt"] = evaluatedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        if (referenceId is not null) resultPayload["referenceId"] = referenceId;
        if (referenceType is not null) resultPayload["referenceType"] = referenceType;

        var resultEvent = EngineEvent.Create(resultEventType, vaultId, resultPayload);

        var events = new[] { requestedEvent, resultEvent };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["riskScore"] = riskScore,
            ["riskLevel"] = riskLevel,
            ["isAllowed"] = isAllowed,
            ["riskDecision"] = riskDecision,
            ["riskReason"] = riskReason,
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
