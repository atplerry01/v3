namespace Whycespace.Runtime.EventFabricGovernance;

public sealed class EventTopicRegistryBuilder
{
    private readonly List<EventTopicDescriptor> _descriptors = [];
    private readonly EventRoutingPolicy _routingPolicy = new();

    public EventTopicRegistryBuilder AddTopic(EventTopicDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        EventTopicValidator.ValidateDescriptor(descriptor);
        _descriptors.Add(descriptor);
        return this;
    }

    public EventTopicRegistryBuilder MapEventToTopic(string eventId, string topicName)
    {
        _routingPolicy.MapEventToTopic(eventId, topicName);
        return this;
    }

    public EventTopicRegistry Build()
    {
        EventTopicValidator.ValidateTopicUniqueness(_descriptors);

        var mappings = _routingPolicy.GetAllMappings();
        foreach (var (eventId, topicName) in mappings)
        {
            if (!_descriptors.Any(d => string.Equals(d.TopicName, topicName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new EventFabricGovernanceException(
                    $"Event '{eventId}' is mapped to topic '{topicName}' which is not registered.");
            }
        }

        return new EventTopicRegistry(_descriptors.AsReadOnly(), _routingPolicy);
    }
}
