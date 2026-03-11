namespace Whycespace.Engines.T3I.Core.Observability;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("Observability", EngineTier.T3I, EngineKind.Projection, "ObservabilityRequest", typeof(EngineEvent))]
public sealed class ObservabilityEngine : IEngine
{
    public string Name => "Observability";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var eventType = context.Data.GetValueOrDefault("eventType") as string;
        if (string.IsNullOrEmpty(eventType))
            return Task.FromResult(EngineResult.Fail("Missing eventType"));

        return eventType switch
        {
            "EngineInvocation" => ProcessEngineInvocation(context),
            "WorkflowExecution" => ProcessWorkflowExecution(context),
            "SystemHealth" => ProcessSystemHealth(context),
            "ErrorRate" => ProcessErrorRate(context),
            _ => ProcessGenericMetric(eventType, context)
        };
    }

    private static Task<EngineResult> ProcessEngineInvocation(EngineContext context)
    {
        var engineName = context.Data.GetValueOrDefault("engineName") as string ?? "unknown";
        var durationMs = ResolveInt64(context.Data.GetValueOrDefault("durationMs")) ?? 0;
        var success = context.Data.GetValueOrDefault("success") is true;

        var latencyBucket = durationMs switch
        {
            < 50 => "fast",
            < 200 => "normal",
            < 1000 => "slow",
            _ => "critical"
        };

        var events = new[]
        {
            EngineEvent.Create("EngineInvocationMetric", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["engineName"] = engineName,
                    ["durationMs"] = durationMs,
                    ["success"] = success,
                    ["latencyBucket"] = latencyBucket,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["metricType"] = "EngineInvocation",
                ["engineName"] = engineName,
                ["latencyBucket"] = latencyBucket,
                ["durationMs"] = durationMs
            }));
    }

    private static Task<EngineResult> ProcessWorkflowExecution(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string ?? "unknown";
        var stepCount = ResolveInt64(context.Data.GetValueOrDefault("stepCount")) ?? 0;
        var totalDurationMs = ResolveInt64(context.Data.GetValueOrDefault("totalDurationMs")) ?? 0;
        var completedSteps = ResolveInt64(context.Data.GetValueOrDefault("completedSteps")) ?? 0;
        var status = context.Data.GetValueOrDefault("status") as string ?? "unknown";

        var throughput = stepCount > 0 && totalDurationMs > 0
            ? Math.Round((double)completedSteps / (totalDurationMs / 1000.0), 2)
            : 0.0;

        var events = new[]
        {
            EngineEvent.Create("WorkflowExecutionMetric", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["workflowName"] = workflowName,
                    ["stepCount"] = stepCount,
                    ["completedSteps"] = completedSteps,
                    ["totalDurationMs"] = totalDurationMs,
                    ["status"] = status,
                    ["throughput"] = throughput,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["metricType"] = "WorkflowExecution",
                ["workflowName"] = workflowName,
                ["throughput"] = throughput,
                ["status"] = status
            }));
    }

    private static Task<EngineResult> ProcessSystemHealth(EngineContext context)
    {
        var component = context.Data.GetValueOrDefault("component") as string ?? "unknown";
        var cpuPercent = ResolveDouble(context.Data.GetValueOrDefault("cpuPercent")) ?? 0.0;
        var memoryPercent = ResolveDouble(context.Data.GetValueOrDefault("memoryPercent")) ?? 0.0;
        var activeWorkflows = ResolveInt64(context.Data.GetValueOrDefault("activeWorkflows")) ?? 0;

        var healthScore = ComputeHealthScore(cpuPercent, memoryPercent);
        var healthStatus = healthScore switch
        {
            >= 0.8 => "healthy",
            >= 0.5 => "degraded",
            _ => "critical"
        };

        var events = new[]
        {
            EngineEvent.Create("SystemHealthMetric", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["component"] = component,
                    ["cpuPercent"] = cpuPercent,
                    ["memoryPercent"] = memoryPercent,
                    ["activeWorkflows"] = activeWorkflows,
                    ["healthScore"] = healthScore,
                    ["healthStatus"] = healthStatus,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["metricType"] = "SystemHealth",
                ["component"] = component,
                ["healthScore"] = healthScore,
                ["healthStatus"] = healthStatus
            }));
    }

    private static Task<EngineResult> ProcessErrorRate(EngineContext context)
    {
        var component = context.Data.GetValueOrDefault("component") as string ?? "unknown";
        var totalInvocations = ResolveInt64(context.Data.GetValueOrDefault("totalInvocations")) ?? 0;
        var failedInvocations = ResolveInt64(context.Data.GetValueOrDefault("failedInvocations")) ?? 0;

        var errorRate = totalInvocations > 0
            ? Math.Round((double)failedInvocations / totalInvocations * 100, 2)
            : 0.0;

        var severity = errorRate switch
        {
            < 1.0 => "normal",
            < 5.0 => "elevated",
            < 20.0 => "high",
            _ => "critical"
        };

        var events = new[]
        {
            EngineEvent.Create("ErrorRateMetric", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["component"] = component,
                    ["totalInvocations"] = totalInvocations,
                    ["failedInvocations"] = failedInvocations,
                    ["errorRate"] = errorRate,
                    ["severity"] = severity,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["metricType"] = "ErrorRate",
                ["component"] = component,
                ["errorRate"] = errorRate,
                ["severity"] = severity
            }));
    }

    private static Task<EngineResult> ProcessGenericMetric(string eventType, EngineContext context)
    {
        var events = new[]
        {
            EngineEvent.Create("GenericMetric", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["data"] = context.Data.Count,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object> { ["metricType"] = eventType }));
    }

    private static double ComputeHealthScore(double cpuPercent, double memoryPercent)
    {
        var cpuScore = Math.Max(0, 1.0 - cpuPercent / 100.0);
        var memScore = Math.Max(0, 1.0 - memoryPercent / 100.0);
        return Math.Round((cpuScore * 0.5) + (memScore * 0.5), 3);
    }

    private static long? ResolveInt64(object? value)
    {
        return value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            decimal m => (long)m,
            string s when long.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static double? ResolveDouble(object? value)
    {
        return value switch
        {
            double d => d,
            decimal m => (double)m,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
