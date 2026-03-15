namespace Whycespace.Engines.T3I.HEOS;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Core.Workforce;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkforceCompliance", EngineTier.T3I, EngineKind.Projection, "WorkforceComplianceCommand", typeof(EngineEvent))]
public sealed class WorkforceComplianceEngine : IEngine
{
    private const decimal FailureRateThreshold = 0.30m;

    public string Name => "WorkforceCompliance";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid compliance command: missing required fields"));

        var workforce = ResolveWorkforce(context);
        if (workforce is null)
            return Task.FromResult(EngineResult.Fail("Missing workforce aggregate data"));

        var decision = EvaluateCompliance(workforce, command);

        var events = new[]
        {
            EngineEvent.Create("WorkforceComplianceEvaluated", command.WorkforceId,
                new Dictionary<string, object>
                {
                    ["workforceId"] = command.WorkforceId.ToString(),
                    ["compliant"] = decision.Compliant,
                    ["complianceScore"] = decision.ComplianceScore,
                    ["violations"] = string.Join(";", decision.Violations),
                    ["recommendations"] = string.Join(";", decision.Recommendations),
                    ["compliancePeriodStart"] = command.CompliancePeriodStart.ToString("O"),
                    ["compliancePeriodEnd"] = command.CompliancePeriodEnd.ToString("O"),
                    ["topic"] = "whyce.heos.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["compliant"] = decision.Compliant,
                ["complianceScore"] = decision.ComplianceScore,
                ["violations"] = decision.Violations,
                ["recommendations"] = decision.Recommendations
            }));
    }

    public static WorkforceComplianceDecision EvaluateCompliance(
        WorkforceAggregate workforce,
        WorkforceComplianceCommand command)
    {
        if (command.CompliancePeriodEnd <= command.CompliancePeriodStart)
            return new WorkforceComplianceDecision(false, 0m,
                new[] { "InvalidCompliancePeriod" },
                new[] { "Provide a valid compliance period where end is after start" });

        var violations = new List<string>();
        var recommendations = new List<string>();
        var score = 100m;

        // Status compliance
        if (!workforce.IsEligible())
        {
            violations.Add("SuspendedWorkerActivity");
            recommendations.Add("Reactivate worker or complete suspension review");
            score -= 40m;
        }

        // Capability compliance - check command capabilities against aggregate
        var missingCapabilities = command.Capabilities
            .Where(c => !workforce.HasCapability(c))
            .ToList();

        if (missingCapabilities.Count > 0)
        {
            violations.Add("CapabilityMismatch");
            recommendations.Add($"Register missing capabilities: {string.Join(", ", missingCapabilities)}");
            score -= 15m * missingCapabilities.Count;
        }

        // Failure rate compliance
        var totalTasks = command.CompletedTasks + command.FailedTasks;
        if (totalTasks > 0)
        {
            var failureRate = (decimal)command.FailedTasks / totalTasks;
            if (failureRate > FailureRateThreshold)
            {
                violations.Add("ExcessiveFailures");
                recommendations.Add($"Failure rate {Math.Round(failureRate * 100, 1)}% exceeds threshold of {FailureRateThreshold * 100}%");
                score -= Math.Round(failureRate * 50m, 2);
            }
        }

        // Policy review compliance
        if (command.LastPolicyReviewDate is null)
        {
            violations.Add("PolicyViolation");
            recommendations.Add("Complete mandatory policy review");
            score -= 20m;
        }
        else
        {
            var periodLength = command.CompliancePeriodEnd - command.CompliancePeriodStart;
            var timeSinceReview = command.CompliancePeriodEnd - command.LastPolicyReviewDate.Value;

            if (timeSinceReview > periodLength + TimeSpan.FromDays(90))
            {
                violations.Add("PolicyViolation");
                recommendations.Add("Policy review is overdue — schedule review immediately");
                score -= 15m;
            }
        }

        score = Math.Max(0m, Math.Min(100m, score));
        var compliant = violations.Count == 0;

        return new WorkforceComplianceDecision(compliant, score, violations, recommendations);
    }

    private static WorkforceComplianceCommand? ResolveCommand(EngineContext context)
    {
        var workforceId = context.Data.GetValueOrDefault("workforceId") as string;
        if (string.IsNullOrEmpty(workforceId) || !Guid.TryParse(workforceId, out var wfGuid))
            return null;

        var workerStatus = context.Data.GetValueOrDefault("workerStatus") as string;
        if (string.IsNullOrEmpty(workerStatus))
            return null;

        var capabilities = context.Data.GetValueOrDefault("capabilities") as IEnumerable<string>;
        var capList = capabilities?.ToList() ?? new List<string>();

        var completedTasks = ResolveInt(context.Data.GetValueOrDefault("completedTasks"));
        var failedTasks = ResolveInt(context.Data.GetValueOrDefault("failedTasks"));
        if (completedTasks is null || failedTasks is null)
            return null;

        var periodStart = ResolveDateTime(context.Data.GetValueOrDefault("compliancePeriodStart"));
        var periodEnd = ResolveDateTime(context.Data.GetValueOrDefault("compliancePeriodEnd"));
        if (periodStart is null || periodEnd is null)
            return null;

        var lastReview = ResolveDateTime(context.Data.GetValueOrDefault("lastPolicyReviewDate"));

        return new WorkforceComplianceCommand(
            wfGuid, workerStatus, capList,
            completedTasks.Value, failedTasks.Value,
            periodStart.Value, periodEnd.Value, lastReview);
    }

    private static WorkforceAggregate? ResolveWorkforce(EngineContext context)
    {
        var workerId = context.Data.GetValueOrDefault("workforceId") as string;
        var workerName = context.Data.GetValueOrDefault("workerName") as string ?? "Worker";
        var workerCapabilities = context.Data.GetValueOrDefault("workerCapabilities") as IEnumerable<string>
            ?? Array.Empty<string>();
        var status = context.Data.GetValueOrDefault("workerStatus") as string ?? "Active";

        if (string.IsNullOrEmpty(workerId) || !Guid.TryParse(workerId, out var wGuid))
            return null;

        var workforce = WorkforceAggregate.Register(new WorkerId(wGuid), workerName, workerCapabilities);

        if (status == "Suspended")
            workforce.Suspend();

        return workforce;
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
