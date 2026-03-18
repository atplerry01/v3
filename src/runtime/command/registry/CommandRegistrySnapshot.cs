namespace Whycespace.CommandSystem.Registry;

public sealed record CommandRegistrySnapshot(
    DateTimeOffset GeneratedAt,
    int TotalCommandCount,
    IReadOnlyList<DomainCommandGroup> Domains
)
{
    public string ToReport()
    {
        var lines = new List<string>
        {
            "=== Command Registry Snapshot ===",
            $"Generated: {GeneratedAt:O}",
            $"Total Commands: {TotalCommandCount}",
            ""
        };

        foreach (var domain in Domains)
        {
            lines.Add($"--- {domain.Domain} ({domain.Commands.Count} command(s)) ---");

            foreach (var command in domain.Commands)
            {
                lines.Add($"  [{command.CommandId}] v{command.Version} -> {command.CommandType.Name}");

                if (command.Description is not null)
                    lines.Add($"    Description: {command.Description}");
            }

            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed record DomainCommandGroup(
    string Domain,
    IReadOnlyList<CommandDescriptor> Commands
);
