namespace Whycespace.Engines.T3I.HEOS;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Workforce;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("WorkforcePerformance", EngineTier.T3I, EngineKind.Projection, "WorkforcePerformanceCommand", typeof(EngineEvent))]
public sealed class WorkforcePerformanceEngine : IEngine
{
    private const decimal CompletionRateWeight = 0.40m;
    private const decimal EfficiencyWeight = 0.25m;
    private const decimal QualityWeight = 0.35m;

    public string Name => "WorkforcePerformance";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid performance command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var decision = EvaluatePerformance(workforce, command);

        var events = new[]
        {
            EngineEvent.Create("WorkforcePerformanceEvaluated", command.WorkforceId,
                new Dictionary<string, object>
                {
                    ["workforceId"] = command.WorkforceId.ToString(),
                    ["performanceScore"] = decision.PerformanceScore,
                    ["performanceTier"] = decision.PerformanceTier.ToString(),
                    ["evaluationSummary"] = decision.EvaluationSummary,
                    ["evaluationPeriodStart"] = command.EvaluationPeriodStart.ToString("O"),
                    ["evaluationPeriodEnd"] = command.EvaluationPeriodEnd.ToString("O"),
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["performanceScore"] = decision.PerformanceScore,
                ["performanceTier"] = decision.PerformanceTier.ToString(),
                ["evaluationSummary"] = decision.EvaluationSummary
            }));
    }

    public static WorkforcePerformanceDecision EvaluatePerformance(
        WorkforceAggregate workforce,
        WorkforcePerformanceCommand command)
    {
        if (command.CompletedTasks < 0)
            return new WorkforcePerformanceDecision(0m, PerformanceTier.Low,
                "Invalid input: CompletedTasks must be >= 0");

        if (command.FailedTasks < 0)
            return new WorkforcePerformanceDecision(0m, PerformanceTier.Low,
                "Invalid input: FailedTasks must be >= 0");

        if (command.CustomerRating < 0 || command.CustomerRating > 5)
            return new WorkforcePerformanceDecision(0m, PerformanceTier.Low,
                "Invalid input: CustomerRating must be between 0 and 5");

        if (command.EvaluationPeriodEnd <= command.EvaluationPeriodStart)
            return new WorkforcePerformanceDecision(0m, PerformanceTier.Low,
                "Invalid input: EvaluationPeriodEnd must be later than EvaluationPeriodStart");

        var totalTasks = command.CompletedTasks + command.FailedTasks;
        var completionRate = totalTasks > 0
            ? (decimal)command.CompletedTasks / totalTasks
            : 0m;

        // Efficiency: normalize average task duration (lower is better)
        // Assume baseline of 60 minutes; score decreases as duration increases
        var efficiencyScore = command.AverageTaskDuration > 0
            ? Math.Max(0m, Math.Min(1m, 60m / command.AverageTaskDuration))
            : 1m;

        // Quality: normalize customer rating to 0-1 scale
        var qualityScore = command.CustomerRating / 5m;

        var performanceScore = Math.Round(
            (completionRate * CompletionRateWeight +
             efficiencyScore * EfficiencyWeight +
             qualityScore * QualityWeight) * 100m, 2);

        var tier = performanceScore switch
        {
            >= 90m => PerformanceTier.Exceptional,
            >= 70m => PerformanceTier.High,
            >= 50m => PerformanceTier.Standard,
            _ => PerformanceTier.Low
        };

        var summary = $"Worker {workforce.Name}: " +
                       $"completed {command.CompletedTasks}/{totalTasks} tasks " +
                       $"(rate: {Math.Round(completionRate * 100, 1)}%), " +
                       $"avg duration: {command.AverageTaskDuration}min, " +
                       $"rating: {command.CustomerRating}/5. " +
                       $"Score: {performanceScore}, Tier: {tier}";

        return new WorkforcePerformanceDecision(performanceScore, tier, summary);
    }

    private static WorkforcePerformanceCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;

        var completedTasks = ResolveInt(context.Data.GetValueOrDefault("completedTasks"));
        var failedTasks = ResolveInt(context.Data.GetValueOrDefault("failedTasks"));
        var avgDuration = ResolveDecimal(context.Data.GetValueOrDefault("averageTaskDuration"));
        var customerRating = ResolveDecimal(context.Data.GetValueOrDefault("customerRating"));

        if (completedTasks is null || failedTasks is null || avgDuration is null || customerRating is null)
            return null;

        var periodStart = ResolveDateTime(context.Data.GetValueOrDefault("evaluationPeriodStart"));
        var periodEnd = ResolveDateTime(context.Data.GetValueOrDefault("evaluationPeriodEnd"));

        if (periodStart is null || periodEnd is null)
            return null;

        return new WorkforcePerformanceCommand(
            wfGuid, completedTasks.Value, failedTasks.Value,
            avgDuration.Value, customerRating.Value,
            periodStart.Value, periodEnd.Value);
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
