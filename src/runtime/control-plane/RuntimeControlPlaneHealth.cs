namespace Whycespace.Runtime.ControlPlane;

using System.Text;
using Whycespace.Runtime.CommandRegistry;
using Whycespace.Runtime.EngineRegistry;
using Whycespace.Runtime.EventFabricGovernance;
using Whycespace.Runtime.EventSchemaRegistry.Snapshot;

public sealed class RuntimeControlPlaneHealth
{
    public EngineRegistrySnapshot EngineSnapshot { get; }
    public CommandRegistrySnapshot CommandSnapshot { get; }
    public EventRegistrySnapshot EventSnapshot { get; }
    public EventFabricSnapshot? FabricSnapshot { get; }

    public RuntimeControlPlaneHealth(
        EngineRegistrySnapshot engineSnapshot,
        CommandRegistrySnapshot commandSnapshot,
        EventRegistrySnapshot eventSnapshot,
        EventFabricSnapshot? fabricSnapshot = null)
    {
        EngineSnapshot = engineSnapshot ?? throw new ArgumentNullException(nameof(engineSnapshot));
        CommandSnapshot = commandSnapshot ?? throw new ArgumentNullException(nameof(commandSnapshot));
        EventSnapshot = eventSnapshot ?? throw new ArgumentNullException(nameof(eventSnapshot));
        FabricSnapshot = fabricSnapshot;
    }

    public static RuntimeControlPlaneHealth FromControlPlane(
        RuntimeControlPlane controlPlane,
        EventFabricSnapshot? fabricSnapshot = null)
    {
        ArgumentNullException.ThrowIfNull(controlPlane);

        return new RuntimeControlPlaneHealth(
            controlPlane.Engines.Snapshot(),
            controlPlane.Commands.CreateSnapshot(),
            controlPlane.Events.CreateSnapshot(),
            fabricSnapshot
        );
    }

    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Runtime Control Plane Snapshot");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();

        sb.AppendLine("Engines:");
        foreach (var tier in Enum.GetValues<EngineTier>())
        {
            var count = EngineSnapshot.ByTier.TryGetValue(tier, out var engines) ? engines.Count : 0;
            sb.AppendLine($"  {tier}: {count}");
        }
        sb.AppendLine();

        sb.AppendLine($"Commands: {CommandSnapshot.TotalCommandCount}");
        sb.AppendLine($"Events: {EventSnapshot.TotalEventCount}");

        if (FabricSnapshot is not null)
        {
            sb.AppendLine($"Kafka Topics: {FabricSnapshot.TotalTopicCount}");
        }

        return sb.ToString();
    }
}
