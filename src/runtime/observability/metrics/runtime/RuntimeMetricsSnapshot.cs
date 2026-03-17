using System.Text;

namespace Whycespace.Runtime.Observability.Metrics.Runtime;

public sealed record RuntimeMetricsSnapshot(
    DateTimeOffset GeneratedAt,
    long CommandsExecutedTotal,
    long CommandsSucceededTotal,
    long CommandsFailedTotal,
    long EngineExecutionsTotal,
    long EngineExecutionsSucceededTotal,
    long EngineExecutionsFailedTotal,
    long EventsPublishedTotal,
    IReadOnlyDictionary<string, long> CommandCountsByType,
    IReadOnlyDictionary<string, long> EngineCountsByName,
    IReadOnlyDictionary<string, long> EngineCountsByTier,
    IReadOnlyDictionary<string, double> EngineLatenciesByName,
    IReadOnlyDictionary<string, long> EventCountsByType,
    IReadOnlyDictionary<string, long> EventCountsByTopic
)
{
    public static RuntimeMetricsSnapshot From(RuntimeMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return new RuntimeMetricsSnapshot(
            GeneratedAt: DateTimeOffset.UtcNow,
            CommandsExecutedTotal: metrics.CommandsExecutedTotal,
            CommandsSucceededTotal: metrics.CommandsSucceededTotal,
            CommandsFailedTotal: metrics.CommandsFailedTotal,
            EngineExecutionsTotal: metrics.EngineExecutionsTotal,
            EngineExecutionsSucceededTotal: metrics.EngineExecutionsSucceededTotal,
            EngineExecutionsFailedTotal: metrics.EngineExecutionsFailedTotal,
            EventsPublishedTotal: metrics.EventsPublishedTotal,
            CommandCountsByType: metrics.GetCommandCountsByType(),
            EngineCountsByName: metrics.GetEngineCountsByName(),
            EngineCountsByTier: metrics.GetEngineCountsByTier(),
            EngineLatenciesByName: metrics.GetEngineLatenciesByName(),
            EventCountsByType: metrics.GetEventCountsByType(),
            EventCountsByTopic: metrics.GetEventCountsByTopic()
        );
    }

    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Runtime Observability Metrics Snapshot");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"Generated: {GeneratedAt:O}");
        sb.AppendLine();

        sb.AppendLine("Commands:");
        foreach (var (type, count) in CommandCountsByType.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {type}: {count}");
        }
        sb.AppendLine($"  Total: {CommandsExecutedTotal} (succeeded: {CommandsSucceededTotal}, failed: {CommandsFailedTotal})");
        sb.AppendLine();

        sb.AppendLine("Engine Executions:");
        foreach (var (name, count) in EngineCountsByName.OrderBy(kvp => kvp.Key))
        {
            var latency = EngineLatenciesByName.GetValueOrDefault(name, 0.0);
            sb.AppendLine($"  {name}: {count} (last latency: {latency:F2}ms)");
        }
        sb.AppendLine($"  Total: {EngineExecutionsTotal} (succeeded: {EngineExecutionsSucceededTotal}, failed: {EngineExecutionsFailedTotal})");
        sb.AppendLine();

        sb.AppendLine("Engine Executions by Tier:");
        foreach (var (tier, count) in EngineCountsByTier.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {tier}: {count}");
        }
        sb.AppendLine();

        sb.AppendLine("Events Published:");
        foreach (var (type, count) in EventCountsByType.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {type}: {count}");
        }
        sb.AppendLine($"  Total: {EventsPublishedTotal}");
        sb.AppendLine();

        sb.AppendLine("Events by Topic:");
        foreach (var (topic, count) in EventCountsByTopic.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"  {topic}: {count}");
        }

        return sb.ToString();
    }
}
