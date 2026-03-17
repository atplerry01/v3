namespace Whycespace.Runtime.EventFabricGovernance;

using System.Text;

public sealed record EventFabricSnapshot(
    DateTimeOffset GeneratedAt,
    int TotalTopicCount,
    IReadOnlyList<DomainTopicGroup> Domains
)
{
    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Event Fabric Snapshot");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();

        foreach (var domain in Domains)
        {
            sb.AppendLine($"  {domain.Domain}: {domain.Topics.Count} topics");
        }

        sb.AppendLine();
        sb.AppendLine($"  Total topics: {TotalTopicCount}");

        return sb.ToString();
    }
}

public sealed record DomainTopicGroup(
    string Domain,
    IReadOnlyList<EventTopicDescriptor> Topics
);
