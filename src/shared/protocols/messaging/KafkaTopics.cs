namespace Whycespace.Shared.Protocols.Messaging;

public static class KafkaTopics
{
    public const string Commands = "whyce.commands";
    public const string WorkflowEvents = "whyce.workflow.events";
    public const string EngineEvents = "whyce.engine.events";
    public const string ClusterEvents = "whyce.cluster.events";
    public const string SpvEvents = "whyce.spv.events";
    public const string EconomicEvents = "whyce.economic.events";
    public const string SystemEvents = "whyce.system.events";
}
