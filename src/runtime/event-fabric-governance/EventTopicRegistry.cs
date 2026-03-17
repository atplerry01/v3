namespace Whycespace.Runtime.EventFabricGovernance;

public sealed class EventTopicRegistry
{
    private readonly Dictionary<string, EventTopicDescriptor> _byTopicName;
    private readonly Dictionary<string, string> _eventIdToTopic;
    private readonly IReadOnlyList<EventTopicDescriptor> _all;

    internal EventTopicRegistry(
        IReadOnlyList<EventTopicDescriptor> descriptors,
        EventRoutingPolicy routingPolicy)
    {
        _all = descriptors;
        _byTopicName = descriptors.ToDictionary(d => d.TopicName, StringComparer.OrdinalIgnoreCase);
        _eventIdToTopic = routingPolicy.GetAllMappings()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public string ResolveTopic(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        if (_eventIdToTopic.TryGetValue(eventId, out var topicName))
            return topicName;

        throw new EventFabricGovernanceException(
            $"No topic mapping found for event '{eventId}'.");
    }

    public EventTopicDescriptor GetTopic(string topicName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        if (_byTopicName.TryGetValue(topicName, out var descriptor))
            return descriptor;

        throw new EventFabricGovernanceException(
            $"No topic registered with name '{topicName}'.");
    }

    public bool HasTopic(string topicName) =>
        _byTopicName.ContainsKey(topicName);

    public IReadOnlyList<EventTopicDescriptor> GetAll() => _all;

    public IReadOnlyList<EventTopicDescriptor> GetByDomain(string domain) =>
        _all.Where(d => string.Equals(d.Domain, domain, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

    public IReadOnlyList<string> GetDomains() =>
        _all.Select(d => d.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

    public int Count => _all.Count;

    public EventFabricSnapshot CreateSnapshot()
    {
        var domains = GetDomains()
            .Select(domain => new DomainTopicGroup(domain, GetByDomain(domain)))
            .ToList()
            .AsReadOnly();

        return new EventFabricSnapshot(
            DateTimeOffset.UtcNow,
            Count,
            domains
        );
    }
}
