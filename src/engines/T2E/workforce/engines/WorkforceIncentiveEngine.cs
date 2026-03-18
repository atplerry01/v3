using Whycespace.Engines.T2E.Workforce.Models;

namespace Whycespace.Engines.T2E.Workforce.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Clusters.Operations.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkforceIncentive", EngineTier.T2E, EngineKind.Mutation, "WorkforceIncentiveCommand", typeof(EngineEvent))]
public sealed class WorkforceIncentiveEngine : IEngine
{
    private static readonly HashSet<string> ValidTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Standard", "High", "Exceptional"
    };

    private static readonly HashSet<string> ValidIncentiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PerformanceBonus", "TaskReward", "RetentionBonus", "OperationalExcellenceReward"
    };

    public string Name => "WorkforceIncentive";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid incentive command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var decision = EvaluateIncentive(workforce, command);

        if (!decision.Eligible)
            return Task.FromResult(EngineResult.Fail(decision.Reason));

        var events = new[]
        {
            EngineEvent.Create("WorkforceIncentiveApproved", command.WorkforceId,
                new Dictionary<string, object>
                {
                    ["workforceId"] = command.WorkforceId.ToString(),
                    ["incentiveAmount"] = decision.IncentiveAmount,
                    ["currency"] = decision.Currency,
                    ["incentiveType"] = decision.IncentiveType,
                    ["payoutReference"] = decision.PayoutReference,
                    ["performanceScore"] = command.PerformanceScore,
                    ["performanceTier"] = command.PerformanceTier,
                    ["evaluationPeriodStart"] = command.EvaluationPeriodStart.ToString("O"),
                    ["evaluationPeriodEnd"] = command.EvaluationPeriodEnd.ToString("O"),
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["eligible"] = decision.Eligible,
                ["incentiveAmount"] = decision.IncentiveAmount,
                ["currency"] = decision.Currency,
                ["incentiveType"] = decision.IncentiveType,
                ["payoutReference"] = decision.PayoutReference,
                ["reason"] = decision.Reason
            }));
    }

    public static WorkforceIncentiveDecision EvaluateIncentive(
        WorkforceAggregate workforce,
        WorkforceIncentiveCommand command)
    {
        if (!workforce.IsEligible())
            return WorkforceIncentiveDecision.Rejected(
                workforce.Status == WorkerStatus.Suspended
                    ? "Suspended workers are not eligible for incentives"
                    : "Worker is not active and not eligible for incentives");

        if (command.EvaluationPeriodEnd <= command.EvaluationPeriodStart)
            return WorkforceIncentiveDecision.Rejected(
                "EvaluationPeriodEnd must be later than EvaluationPeriodStart");

        if (command.PerformanceScore < 0 || command.PerformanceScore > 100)
            return WorkforceIncentiveDecision.Rejected(
                "PerformanceScore must be between 0 and 100");

        if (command.BaseIncentiveAmount < 0)
            return WorkforceIncentiveDecision.Rejected(
                "BaseIncentiveAmount must be >= 0");

        if (!ValidTiers.Contains(command.PerformanceTier))
            return WorkforceIncentiveDecision.Rejected(
                $"Unknown performance tier '{command.PerformanceTier}'");

        if (!ValidIncentiveTypes.Contains(command.IncentiveType))
            return WorkforceIncentiveDecision.Rejected(
                $"Unknown incentive type '{command.IncentiveType}'");

        var multiplier = GetMultiplier(command.PerformanceTier);
        var incentiveAmount = Math.Round(command.BaseIncentiveAmount * multiplier, 2);

        var payoutReference = GeneratePayoutReference(
            command.WorkforceId, command.EvaluationPeriodStart);

        return WorkforceIncentiveDecision.Success(
            incentiveAmount, command.Currency, command.IncentiveType, payoutReference);
    }

    private static decimal GetMultiplier(string tier)
    {
        return tier.ToLowerInvariant() switch
        {
            "low" => 0.0m,
            "standard" => 1.0m,
            "high" => 1.25m,
            "exceptional" => 1.5m,
            _ => 0.0m
        };
    }

    private static string GeneratePayoutReference(Guid workforceId, DateTimeOffset periodStart)
    {
        var input = $"{workforceId}-{periodStart:yyyyMMdd}";
        var hashBytes = global::System.Security.Cryptography.SHA256.HashData(
            global::System.Text.Encoding.UTF8.GetBytes(input));
        var shortHash = BitConverter.ToUInt64(hashBytes, 0);
        return $"PAYOUT-{shortHash:X16}";
    }

    private static WorkforceIncentiveCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        var performanceTier = context.Data.GetValueOrDefault("performanceTier") as string;
        var currency = context.Data.GetValueOrDefault("currency") as string;
        var incentiveType = context.Data.GetValueOrDefault("incentiveType") as string;

        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;
        if (string.IsNullOrEmpty(performanceTier))
            return null;
        if (string.IsNullOrEmpty(currency))
            return null;
        if (string.IsNullOrEmpty(incentiveType))
            return null;

        var performanceScore = ResolveDecimal(context.Data.GetValueOrDefault("performanceScore"));
        var baseAmount = ResolveDecimal(context.Data.GetValueOrDefault("baseIncentiveAmount"));

        if (performanceScore is null || baseAmount is null)
            return null;

        var periodStart = ResolveDateTime(context.Data.GetValueOrDefault("evaluationPeriodStart"));
        var periodEnd = ResolveDateTime(context.Data.GetValueOrDefault("evaluationPeriodEnd"));

        if (periodStart is null || periodEnd is null)
            return null;

        return new WorkforceIncentiveCommand(
            wfGuid, performanceScore.Value, performanceTier,
            periodStart.Value, periodEnd.Value,
            baseAmount.Value, currency, incentiveType);
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var capabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();
        var status = context.Data.GetValueOrDefault("workerStatus") as string ?? "Active";

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        var workforce = WorkforceAggregate.Register(new WorkerId(wGuid), workerName, capabilities);

        if (status == "Suspended")
            workforce.Suspend();

        return workforce;
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

    private static DateTimeOffset? ResolveDateTime(object? value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            string s when DateTimeOffset.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
