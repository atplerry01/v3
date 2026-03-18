using Whycespace.Platform.Simulation.Runner;
using Whycespace.Platform.Simulation.Scenarios;
using Whycespace.Platform.Simulation.Metrics;
using Whycespace.Platform.Simulation.Reporting;

var parsed = CliParser.Parse(args);

var config = ResolveScenario(parsed.Scenario, parsed.Workers, parsed.DurationSec, parsed.FaultRate);
var metrics = new SimulationMetrics();
var runner = new SimulationRunner(config, metrics);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await runner.RunAsync(cts.Token);

var report = new SimulationReport(config, metrics);
report.PrintToConsole();

if (!string.IsNullOrEmpty(parsed.Output))
    await report.SaveToJsonAsync(parsed.Output);

return 0;

static SimulationConfig ResolveScenario(string scenario, int workers, int? durationSec, double faultRate)
{
    var duration = durationSec.HasValue ? TimeSpan.FromSeconds(durationSec.Value) : (TimeSpan?)null;

    return scenario.ToLowerInvariant() switch
    {
        "small" => SimulationScenario.Small(workers, faultRate) with { Duration = duration },
        "medium" => SimulationScenario.Medium(workers, faultRate) with { Duration = duration },
        "large" => SimulationScenario.Large(workers, faultRate) with { Duration = duration },
        _ when int.TryParse(scenario, out var count) => SimulationScenario.Custom(count, workers, duration, faultRate),
        _ => throw new ArgumentException($"Unknown scenario: {scenario}. Use small, medium, large, or a number.")
    };
}

internal sealed record CliArgs(
    string Scenario,
    int Workers,
    int? DurationSec,
    double FaultRate,
    string? Output
);

internal static class CliParser
{
    public static CliArgs Parse(string[] args)
    {
        var scenario = "small";
        var workers = 10;
        int? duration = null;
        var faultRate = 0.0;
        string? output = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--scenario" or "-s" when i + 1 < args.Length:
                    scenario = args[++i];
                    break;
                case "--workers" or "-w" when i + 1 < args.Length:
                    workers = int.Parse(args[++i]);
                    break;
                case "--duration" or "-d" when i + 1 < args.Length:
                    duration = int.Parse(args[++i]);
                    break;
                case "--fault-rate" or "-f" when i + 1 < args.Length:
                    faultRate = double.Parse(args[++i]);
                    break;
                case "--output" or "-o" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--help" or "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return new CliArgs(scenario, workers, duration, faultRate, output);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Whycespace WBSM v3 Load Simulation Framework");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -s, --scenario <name>     small (1K), medium (50K), large (1M), or a number");
        Console.WriteLine("  -w, --workers <count>     Concurrency level (default: 10)");
        Console.WriteLine("  -d, --duration <seconds>  Maximum duration in seconds");
        Console.WriteLine("  -f, --fault-rate <rate>   Fault injection rate 0.0-1.0 (default: 0)");
        Console.WriteLine("  -o, --output <path>       JSON report output file path");
        Console.WriteLine("  -h, --help                Show this help");
    }
}