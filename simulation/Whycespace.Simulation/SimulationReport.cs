namespace Whycespace.Simulation;

using global::System.Text.Json;
using global::System.Text.Json.Serialization;

public sealed class SimulationReport
{
    private readonly SimulationConfig _config;
    private readonly SimulationMetrics _metrics;

    public SimulationReport(SimulationConfig config, SimulationMetrics metrics)
    {
        _config = config;
        _metrics = metrics;
    }

    public void PrintToConsole()
    {
        var divider = new string('─', 60);

        Console.WriteLine();
        Console.WriteLine(divider);
        Console.WriteLine("  WHYCESPACE WBSM v3 — LOAD SIMULATION REPORT");
        Console.WriteLine(divider);
        Console.WriteLine();

        PrintSection("Scenario Configuration");
        PrintRow("Scenario", _config.Name);
        PrintRow("Target workflows", $"{_config.TotalWorkflows:N0}");
        PrintRow("Concurrency workers", $"{_config.Workers}");
        PrintRow("Fault injection rate", $"{_config.FaultRate:P1}");
        PrintRow("Duration limit", _config.Duration?.ToString(@"hh\:mm\:ss") ?? "unlimited");
        Console.WriteLine();

        PrintSection("Execution Summary");
        PrintRow("Workflows executed", $"{_metrics.TotalExecuted:N0}");
        PrintRow("Succeeded", $"{_metrics.TotalSucceeded:N0}");
        PrintRow("Failed", $"{_metrics.TotalFailed:N0}");
        PrintRow("Success rate", $"{_metrics.SuccessRate:F2}%");
        PrintRow("Wall clock time", $"{_metrics.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine();

        PrintSection("Throughput");
        PrintRow("Workflows/sec", $"{_metrics.WorkflowsPerSecond:N0}");
        PrintRow("Engine invocations/sec", $"{_metrics.EngineInvocationsPerSecond:N0}");
        PrintRow("Events published/sec", $"{_metrics.EventsPerSecond:N0}");
        Console.WriteLine();

        PrintSection("Latency — Workflow");
        PrintRow("Average", $"{_metrics.AverageWorkflowLatencyMs:F2} ms");
        PrintRow("P95", $"{_metrics.P95WorkflowLatencyMs:F2} ms");
        PrintRow("P99", $"{_metrics.P99WorkflowLatencyMs:F2} ms");
        PrintRow("Max", $"{_metrics.MaxWorkflowLatencyMs:F2} ms");
        Console.WriteLine();

        PrintSection("Latency — Engine");
        PrintRow("Average engine", $"{_metrics.AverageEngineLatencyMs:F4} ms");
        Console.WriteLine();

        PrintSection("Latency — Projection");
        PrintRow("Average projection", $"{_metrics.AverageProjectionLatencyMs:F4} ms");
        Console.WriteLine();

        PrintSection("Reliability");
        PrintRow("Total engine invocations", $"{_metrics.TotalEngineInvocations:N0}");
        PrintRow("Events published", $"{_metrics.TotalEventsPublished:N0}");
        PrintRow("Projection updates", $"{_metrics.TotalProjectionUpdates:N0}");
        PrintRow("Retries attempted", $"{_metrics.TotalRetries:N0}");
        PrintRow("Dead lettered", $"{_metrics.TotalDeadLettered:N0}");
        Console.WriteLine();

        PrintSection("Workflow Distribution");
        foreach (var kvp in _metrics.WorkflowTypeCounts.OrderByDescending(x => x.Value))
            PrintRow($"  {kvp.Key}", $"{kvp.Value:N0}");
        Console.WriteLine();

        PrintSection("Engine Invocation Distribution");
        foreach (var kvp in _metrics.EngineInvocationCounts.OrderByDescending(x => x.Value))
            PrintRow($"  {kvp.Key}", $"{kvp.Value:N0}");

        Console.WriteLine();
        Console.WriteLine(divider);
    }

    public async Task SaveToJsonAsync(string path)
    {
        var report = new
        {
            Scenario = new
            {
                _config.Name,
                _config.TotalWorkflows,
                _config.Workers,
                _config.FaultRate,
                Duration = _config.Duration?.ToString()
            },
            Execution = new
            {
                _metrics.TotalExecuted,
                _metrics.TotalSucceeded,
                _metrics.TotalFailed,
                _metrics.SuccessRate,
                WallClockSeconds = _metrics.Elapsed.TotalSeconds
            },
            Throughput = new
            {
                WorkflowsPerSecond = Math.Round(_metrics.WorkflowsPerSecond, 2),
                EngineInvocationsPerSecond = Math.Round(_metrics.EngineInvocationsPerSecond, 2),
                EventsPerSecond = Math.Round(_metrics.EventsPerSecond, 2)
            },
            WorkflowLatency = new
            {
                AverageMs = Math.Round(_metrics.AverageWorkflowLatencyMs, 4),
                P95Ms = Math.Round(_metrics.P95WorkflowLatencyMs, 4),
                P99Ms = Math.Round(_metrics.P99WorkflowLatencyMs, 4),
                MaxMs = Math.Round(_metrics.MaxWorkflowLatencyMs, 4)
            },
            EngineLatency = new
            {
                AverageMs = Math.Round(_metrics.AverageEngineLatencyMs, 4)
            },
            ProjectionLatency = new
            {
                AverageMs = Math.Round(_metrics.AverageProjectionLatencyMs, 4)
            },
            Reliability = new
            {
                _metrics.TotalEngineInvocations,
                _metrics.TotalEventsPublished,
                _metrics.TotalProjectionUpdates,
                _metrics.TotalRetries,
                _metrics.TotalDeadLettered
            },
            WorkflowDistribution = _metrics.WorkflowTypeCounts.ToDictionary(x => x.Key, x => x.Value),
            EngineDistribution = _metrics.EngineInvocationCounts.ToDictionary(x => x.Key, x => x.Value),
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"[Report] Saved to {path}");
    }

    private static void PrintSection(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
    }

    private static void PrintRow(string label, string value)
    {
        Console.Write($"    {label,-32}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(value);
        Console.ResetColor();
    }
}
