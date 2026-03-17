namespace Whycespace.Runtime.ControlPlane;

using Whycespace.Runtime.CommandRegistry;
using Whycespace.Runtime.EngineRegistry;
using Whycespace.Runtime.EventSchemaRegistry.Registry;
using Whycespace.Runtime.EventSchemaRegistry.Snapshot;

public sealed class RuntimeControlPlane
{
    public CommandRegistry Commands { get; }
    public EngineRegistry Engines { get; }
    public EventRegistry Events { get; }

    internal RuntimeControlPlane(
        CommandRegistry commands,
        EngineRegistry engines,
        EventRegistry events)
    {
        Commands = commands ?? throw new ArgumentNullException(nameof(commands));
        Engines = engines ?? throw new ArgumentNullException(nameof(engines));
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public RuntimeControlPlaneSnapshot Snapshot()
    {
        var engineSnapshot = Engines.Snapshot();
        var commandSnapshot = Commands.CreateSnapshot();
        var eventSnapshot = Events.CreateSnapshot();

        var engineCountsByTier = engineSnapshot.ByTier
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        var commandCountsByDomain = commandSnapshot.Domains
            .ToDictionary(d => d.Domain, d => d.Commands.Count);

        var eventCountsByDomain = eventSnapshot.Domains
            .ToDictionary(d => d.Domain, d => d.Events.Count);

        return new RuntimeControlPlaneSnapshot(
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalEngines: engineSnapshot.TotalCount,
            TotalCommands: commandSnapshot.TotalCommandCount,
            TotalEvents: eventSnapshot.TotalEventCount,
            EngineCountsByTier: engineCountsByTier.AsReadOnly(),
            CommandCountsByDomain: commandCountsByDomain.AsReadOnly(),
            EventCountsByDomain: eventCountsByDomain.AsReadOnly()
        );
    }
}
