namespace Whycespace.Runtime.ControlPlane;

using System.Text;
using Whycespace.Runtime.EngineRegistry;

public sealed record RuntimeControlPlaneSnapshot(
    DateTimeOffset GeneratedAt,
    int TotalEngines,
    int TotalCommands,
    int TotalEvents,
    IReadOnlyDictionary<EngineTier, int> EngineCountsByTier,
    IReadOnlyDictionary<string, int> CommandCountsByDomain,
    IReadOnlyDictionary<string, int> EventCountsByDomain
)
{
    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Runtime Control Plane Snapshot");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"Generated: {GeneratedAt:O}");
        sb.AppendLine();

        sb.AppendLine("Engines:");
        foreach (var (tier, count) in EngineCountsByTier.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {tier}: {count}");
        }
        sb.AppendLine($"  Total: {TotalEngines}");
        sb.AppendLine();

        sb.AppendLine("Commands:");
        foreach (var (domain, count) in CommandCountsByDomain.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {domain}: {count}");
        }
        sb.AppendLine($"  Total: {TotalCommands}");
        sb.AppendLine();

        sb.AppendLine("Events:");
        foreach (var (domain, count) in EventCountsByDomain.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {domain}: {count}");
        }
        sb.AppendLine($"  Total: {TotalEvents}");

        return sb.ToString();
    }
}
