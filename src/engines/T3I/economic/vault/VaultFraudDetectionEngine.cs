namespace Whycespace.Engines.T3I.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultFraudDetection", EngineTier.T3I, EngineKind.Decision, "EvaluateVaultFraudCommand", typeof(EngineEvent))]
public sealed class VaultFraudDetectionEngine : IEngine
{
    private const double FraudAlertThreshold = 60.0;

    public string Name => "VaultFraudDetection";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate required inputs ---
        if (!context.Data.TryGetValue("vaultId", out var vaultIdObj) ||
            !Guid.TryParse(vaultIdObj?.ToString(), out var vaultId))
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultId"));

        if (!context.Data.TryGetValue("transactionId", out var txIdObj) ||
            !Guid.TryParse(txIdObj?.ToString(), out var transactionId))
            return Task.FromResult(EngineResult.Fail("Missing or invalid transactionId"));

        if (!context.Data.TryGetValue("vaultAccountId", out var acctIdObj) ||
            !Guid.TryParse(acctIdObj?.ToString(), out _))
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultAccountId"));

        if (!context.Data.TryGetValue("initiatorIdentityId", out var identIdObj) ||
            !Guid.TryParse(identIdObj?.ToString(), out _))
            return Task.FromResult(EngineResult.Fail("Missing or invalid initiatorIdentityId"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null || amount < 0)
            return Task.FromResult(EngineResult.Fail("Missing or invalid amount"));

        var operationType = context.Data.GetValueOrDefault("operationType")?.ToString();
        if (string.IsNullOrWhiteSpace(operationType))
            return Task.FromResult(EngineResult.Fail("Missing operationType"));

        var currency = context.Data.GetValueOrDefault("currency")?.ToString();
        if (string.IsNullOrWhiteSpace(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));

        // --- Resolve behavioral context (optional inputs for fraud analysis) ---
        var recentTransactionCount = ResolveInt(context.Data.GetValueOrDefault("recentTransactionCount")) ?? 0;
        var recentTransactionWindowMinutes = ResolveInt(context.Data.GetValueOrDefault("recentTransactionWindowMinutes")) ?? 60;
        var averageTransactionAmount = ResolveDecimal(context.Data.GetValueOrDefault("averageTransactionAmount")) ?? 0m;
        var recentFailedOperationCount = ResolveInt(context.Data.GetValueOrDefault("recentFailedOperationCount")) ?? 0;
        var recentLargeWithdrawalCount = ResolveInt(context.Data.GetValueOrDefault("recentLargeWithdrawalCount")) ?? 0;
        var accountAgeDays = ResolveInt(context.Data.GetValueOrDefault("accountAgeDays")) ?? 365;
        var isNewIdentity = ResolveBool(context.Data.GetValueOrDefault("isNewIdentity"));

        // --- Compute fraud signals ---
        var fraudFactors = new Dictionary<string, double>();
        var reasons = new List<string>();

        // Signal 1: Transaction velocity
        if (recentTransactionCount > 20 && recentTransactionWindowMinutes <= 60)
        {
            var velocityScore = Math.Min((recentTransactionCount - 20) * 2.0, 25.0);
            fraudFactors["transactionVelocity"] = velocityScore;
            reasons.Add($"High transaction velocity: {recentTransactionCount} transactions in {recentTransactionWindowMinutes} minutes");
        }
        else if (recentTransactionCount > 10 && recentTransactionWindowMinutes <= 60)
        {
            var velocityScore = Math.Min((recentTransactionCount - 10) * 1.0, 10.0);
            fraudFactors["transactionVelocity"] = velocityScore;
            reasons.Add($"Elevated transaction velocity: {recentTransactionCount} transactions in {recentTransactionWindowMinutes} minutes");
        }

        // Signal 2: Abnormal withdrawal behavior
        if (averageTransactionAmount > 0 && amount.Value > averageTransactionAmount * 5)
        {
            var ratio = (double)(amount.Value / averageTransactionAmount);
            var abnormalScore = Math.Min(ratio * 3.0, 25.0);
            fraudFactors["abnormalAmount"] = abnormalScore;
            reasons.Add($"Transaction amount {amount.Value} is {ratio:F1}x the average ({averageTransactionAmount})");
        }
        else if (averageTransactionAmount > 0 && amount.Value > averageTransactionAmount * 3)
        {
            var ratio = (double)(amount.Value / averageTransactionAmount);
            var abnormalScore = Math.Min(ratio * 1.5, 12.0);
            fraudFactors["abnormalAmount"] = abnormalScore;
            reasons.Add($"Transaction amount {amount.Value} is {ratio:F1}x the average ({averageTransactionAmount})");
        }

        // Signal 3: Repeated failed operations (probing attempts)
        if (recentFailedOperationCount > 5)
        {
            var probeScore = Math.Min((recentFailedOperationCount - 5) * 4.0, 20.0);
            fraudFactors["failedOperations"] = probeScore;
            reasons.Add($"High failed operation count: {recentFailedOperationCount} recent failures");
        }
        else if (recentFailedOperationCount > 2)
        {
            var probeScore = Math.Min((recentFailedOperationCount - 2) * 2.0, 8.0);
            fraudFactors["failedOperations"] = probeScore;
            reasons.Add($"Elevated failed operation count: {recentFailedOperationCount} recent failures");
        }

        // Signal 4: Vault drain attempts (multiple large withdrawals)
        if (recentLargeWithdrawalCount > 3 &&
            (operationType.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase) ||
             operationType.Equals("Transfer", StringComparison.OrdinalIgnoreCase)))
        {
            var drainScore = Math.Min((recentLargeWithdrawalCount - 3) * 5.0, 25.0);
            fraudFactors["vaultDrainAttempt"] = drainScore;
            reasons.Add($"Potential vault drain: {recentLargeWithdrawalCount} recent large withdrawals");
        }
        else if (recentLargeWithdrawalCount > 1 &&
                 operationType.Equals("Withdrawal", StringComparison.OrdinalIgnoreCase))
        {
            var drainScore = Math.Min((recentLargeWithdrawalCount - 1) * 3.0, 10.0);
            fraudFactors["vaultDrainAttempt"] = drainScore;
            reasons.Add($"Multiple large withdrawals detected: {recentLargeWithdrawalCount}");
        }

        // Signal 5: Identity pattern anomalies
        if (isNewIdentity && amount.Value > 10_000)
        {
            var identityScore = Math.Min((double)(amount.Value / 10_000m) * 5.0, 15.0);
            fraudFactors["newIdentityHighAmount"] = identityScore;
            reasons.Add($"New identity with high-value transaction: {amount.Value} {currency}");
        }

        if (accountAgeDays < 7 && amount.Value > 5_000)
        {
            var newAccountScore = Math.Min((double)(amount.Value / 5_000m) * 4.0, 15.0);
            fraudFactors["newAccountActivity"] = newAccountScore;
            reasons.Add($"Account age {accountAgeDays} days with transaction amount {amount.Value}");
        }

        // --- Aggregate fraud score ---
        var rawScore = fraudFactors.Values.Sum();
        var fraudScore = Math.Clamp(rawScore, 0.0, 100.0);

        // --- Classify risk level ---
        var fraudRiskLevel = fraudScore switch
        {
            <= 25.0 => "Low",
            <= 60.0 => "Suspicious",
            _ => "HighFraudRisk"
        };

        var fraudAlertTriggered = fraudScore > FraudAlertThreshold;
        var fraudReason = reasons.Count > 0
            ? string.Join("; ", reasons)
            : "No fraud signals detected";

        // --- Build output ---
        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["fraudScore"] = fraudScore,
            ["fraudRiskLevel"] = fraudRiskLevel,
            ["fraudAlertTriggered"] = fraudAlertTriggered,
            ["fraudReason"] = fraudReason,
            ["fraudFactors"] = fraudFactors,
            ["evaluatedAt"] = DateTime.UtcNow.ToString("O")
        };

        // --- Emit events ---
        var events = new List<EngineEvent>();

        var evaluationPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["transactionId"] = transactionId.ToString(),
            ["fraudScore"] = fraudScore,
            ["fraudRiskLevel"] = fraudRiskLevel,
            ["fraudAlertTriggered"] = fraudAlertTriggered,
            ["operationType"] = operationType,
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["topic"] = "whyce.economic.events"
        };

        events.Add(EngineEvent.Create("VaultFraudEvaluationCompleted", vaultId, evaluationPayload));

        if (fraudAlertTriggered)
        {
            var alertPayload = new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["transactionId"] = transactionId.ToString(),
                ["fraudScore"] = fraudScore,
                ["fraudRiskLevel"] = fraudRiskLevel,
                ["fraudReason"] = fraudReason,
                ["topic"] = "whyce.economic.events"
            };
            events.Add(EngineEvent.Create("VaultFraudAlertTriggered", vaultId, alertPayload));
        }

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
            double d => (int)d,
            decimal m => (int)m,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static bool ResolveBool(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            int i => i != 0,
            _ => false
        };
    }
}
