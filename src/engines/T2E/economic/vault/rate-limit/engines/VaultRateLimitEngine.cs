namespace Whycespace.Engines.T2E.Economic.Vault.RateLimit.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultRateLimit", EngineTier.T2E, EngineKind.Validation, "EvaluateVaultRateLimitCommand", typeof(EngineEvent))]
public sealed class VaultRateLimitEngine : IEngine
{
    public string Name => "VaultRateLimit";

    private static readonly string[] SupportedOperationTypes =
    {
        "Transfer", "Withdrawal", "Contribution", "Distribution", "ProfitDistribution",
        "Adjustment", "Refund"
    };

    private static readonly Dictionary<string, (int MaxOperations, TimeSpan Window)> RateLimitRules = new()
    {
        ["Transfer"] = (20, TimeSpan.FromHours(1)),
        ["Withdrawal"] = (5, TimeSpan.FromHours(1)),
        ["Contribution"] = (100, TimeSpan.FromHours(1)),
        ["Distribution"] = (10, TimeSpan.FromHours(1)),
        ["ProfitDistribution"] = (10, TimeSpan.FromHours(1)),
        ["Adjustment"] = (10, TimeSpan.FromHours(1)),
        ["Refund"] = (5, TimeSpan.FromHours(1))
    };

    private const double WarningThresholdPercent = 0.8;

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdRaw = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdRaw, out _) || Guid.Parse(vaultAccountIdRaw) == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdRaw, out _) || Guid.Parse(initiatorIdRaw) == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

        // --- Validate OperationType ---
        var operationType = context.Data.GetValueOrDefault("operationType") as string;
        if (string.IsNullOrEmpty(operationType))
            return Task.FromResult(EngineResult.Fail("Missing operationType"));
        if (!Array.Exists(SupportedOperationTypes, t => t == operationType))
            return Task.FromResult(EngineResult.Fail(
                $"Unsupported operationType: {operationType}. Supported: {string.Join(", ", SupportedOperationTypes)}"));

        // --- Validate CurrentOperationCount ---
        var currentCount = ResolveInt(context.Data.GetValueOrDefault("currentOperationCount"));
        if (currentCount is null || currentCount.Value < 0)
            return Task.FromResult(EngineResult.Fail("Missing or invalid currentOperationCount"));

        // --- Resolve rate limit rule ---
        var (maxOperations, windowDuration) = RateLimitRules[operationType];

        // --- Evaluate rate limit ---
        var countAfterOperation = currentCount.Value + 1;
        var warningThreshold = (int)Math.Ceiling(maxOperations * WarningThresholdPercent);

        string rateLimitStatus;
        string rateLimitReason;
        bool isAllowed;

        if (countAfterOperation > maxOperations)
        {
            rateLimitStatus = "Blocked";
            rateLimitReason = $"Rate limit exceeded for '{operationType}': {currentCount.Value} operations in window (max {maxOperations} per {windowDuration.TotalHours}h)";
            isAllowed = false;
        }
        else if (currentCount.Value >= warningThreshold)
        {
            rateLimitStatus = "Warning";
            rateLimitReason = $"Approaching rate limit for '{operationType}': {currentCount.Value} of {maxOperations} operations used in window";
            isAllowed = true;
        }
        else
        {
            rateLimitStatus = "Allowed";
            rateLimitReason = $"Operation '{operationType}' within rate limit: {currentCount.Value} of {maxOperations} operations used in window";
            isAllowed = true;
        }

        var evaluatedAt = DateTime.UtcNow;

        // --- Optional metadata ---
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string ?? "";

        // --- Emit events ---
        var requestedPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["vaultAccountId"] = vaultAccountIdRaw,
            ["initiatorIdentityId"] = initiatorIdRaw,
            ["operationType"] = operationType,
            ["currentOperationCount"] = currentCount.Value,
            ["maxAllowedOperations"] = maxOperations,
            ["referenceId"] = referenceId,
            ["referenceType"] = referenceType,
            ["topic"] = "whyce.economic.events"
        };

        var evaluationEventType = rateLimitStatus switch
        {
            "Blocked" => "VaultRateLimitExceeded",
            "Warning" => "VaultRateLimitWarning",
            _ => "VaultRateLimitEvaluationPassed"
        };

        var evaluationPayload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["operationType"] = operationType,
            ["currentOperationCount"] = currentCount.Value,
            ["maxAllowedOperations"] = maxOperations,
            ["windowDurationMinutes"] = windowDuration.TotalMinutes,
            ["isAllowed"] = isAllowed,
            ["rateLimitStatus"] = rateLimitStatus,
            ["rateLimitReason"] = rateLimitReason,
            ["evaluatedAt"] = evaluatedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        var events = new[]
        {
            EngineEvent.Create("VaultRateLimitEvaluationRequested", vaultId, requestedPayload),
            EngineEvent.Create(evaluationEventType, vaultId, evaluationPayload)
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["operationType"] = operationType,
            ["currentOperationCount"] = currentCount.Value,
            ["maxAllowedOperations"] = maxOperations,
            ["windowDurationMinutes"] = windowDuration.TotalMinutes,
            ["isAllowed"] = isAllowed,
            ["rateLimitStatus"] = rateLimitStatus,
            ["rateLimitReason"] = rateLimitReason,
            ["evaluatedAt"] = evaluatedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static int? ResolveInt(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double d => (int)d,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
