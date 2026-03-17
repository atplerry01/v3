namespace Whycespace.Runtime.EventSchemaRegistry.Snapshot;

using System.Text;
using Whycespace.Runtime.EventSchemaRegistry.Models;

public sealed record EventRegistrySnapshot(
    DateTimeOffset GeneratedAt,
    int TotalEventCount,
    IReadOnlyList<DomainEventGroup> Domains
)
{
    public string ToReport()
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Event Schema Registry Snapshot ===");
        sb.AppendLine($"Generated: {GeneratedAt:O}");
        sb.AppendLine($"Total Events: {TotalEventCount}");
        sb.AppendLine($"Domains: {Domains.Count}");
        sb.AppendLine();

        foreach (var domain in Domains)
        {
            sb.AppendLine($"--- {domain.Domain} ({domain.Events.Count} events) ---");

            foreach (var evt in domain.Events)
            {
                sb.AppendLine($"  [{evt.EventId}] v{evt.Version} — {evt.EventType.Name}");

                if (evt.Description is not null)
                    sb.AppendLine($"    Description: {evt.Description}");

                foreach (var prop in evt.Properties)
                {
                    var required = prop.IsRequired ? " (required)" : "";
                    sb.AppendLine($"    • {prop.Name}: {prop.TypeName}{required}");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

public sealed record DomainEventGroup(
    string Domain,
    IReadOnlyList<EventDescriptor> Events
);
