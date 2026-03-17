namespace Whycespace.Runtime.EventFabricGovernance;

using System.Text.RegularExpressions;

public static partial class EventTopicValidator
{
    private static readonly Regex TopicNamePattern = TopicNameRegex();

    public static void ValidateTopicName(string topicName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        if (!TopicNamePattern.IsMatch(topicName))
        {
            throw new EventFabricGovernanceException(
                $"Topic name '{topicName}' does not follow the required pattern '<domain>.<entity>.<action>' " +
                "(e.g., 'capital.contribution.recorded').");
        }
    }

    public static void ValidateTopicUniqueness(IReadOnlyList<EventTopicDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(d => d.TopicName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new EventFabricGovernanceException(
                $"Duplicate topic names detected: {string.Join(", ", duplicates)}.");
        }
    }

    public static void ValidateDescriptor(EventTopicDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        ValidateTopicName(descriptor.TopicName);
        EventRetentionPolicy.Validate(descriptor.Retention);

        if (descriptor.PartitionCount < 1)
        {
            throw new EventFabricGovernanceException(
                $"Topic '{descriptor.TopicName}' must have at least 1 partition.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.Domain))
        {
            throw new EventFabricGovernanceException(
                $"Topic '{descriptor.TopicName}' must have a domain assigned.");
        }
    }

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){2,}$", RegexOptions.Compiled)]
    private static partial Regex TopicNameRegex();
}
