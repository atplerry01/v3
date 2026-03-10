namespace Whycespace.EventFabric.Topics;

public static class EventTopics
{
    public const string Commands = "whyce.commands";

    public const string WorkflowEvents = "whyce.workflow.events";

    public const string EngineEvents = "whyce.engine.events";

    public const string ClusterEvents = "whyce.cluster.events";

    public const string SpvEvents = "whyce.spv.events";

    public const string EconomicEvents = "whyce.economic.events";

    public const string SystemEvents = "whyce.system.events";

    public static IReadOnlyList<string> All =>
    [
        Commands,
        WorkflowEvents,
        EngineEvents,
        ClusterEvents,
        SpvEvents,
        EconomicEvents,
        SystemEvents
    ];
}
