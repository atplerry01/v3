namespace Whycespace.Runtime.EventFabricGovernance;

public sealed class EventRoutingPolicy
{
    private readonly Dictionary<string, string> _eventToTopic = new();

    public void MapEventToTopic(string eventId, string topicName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        if (_eventToTopic.TryGetValue(eventId, out var existing))
        {
            throw new EventFabricGovernanceException(
                $"Event '{eventId}' is already mapped to topic '{existing}'. Cannot remap to '{topicName}'.");
        }

        _eventToTopic[eventId] = topicName;
    }

    public string ResolveTopic(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        if (_eventToTopic.TryGetValue(eventId, out var topic))
            return topic;

        throw new EventFabricGovernanceException(
            $"No topic mapping found for event '{eventId}'.");
    }

    public bool HasMapping(string eventId) =>
        _eventToTopic.ContainsKey(eventId);

    public IReadOnlyDictionary<string, string> GetAllMappings() =>
        _eventToTopic.AsReadOnly();
}
