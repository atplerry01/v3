namespace Whycespace.Runtime.EventFabricGovernance;

public sealed record EventTopicDescriptor(
    string TopicName,
    string Domain,
    int PartitionCount,
    TimeSpan Retention,
    IReadOnlyList<string> EventIds
)
{
    public EventTopicDescriptor(string topicName, string domain, int partitionCount, TimeSpan retention)
        : this(topicName, domain, partitionCount, retention, Array.Empty<string>()) { }
}
