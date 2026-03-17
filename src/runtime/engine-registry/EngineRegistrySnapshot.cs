namespace Whycespace.Runtime.EngineRegistry;

using System.Text;

public sealed class EngineRegistrySnapshot
{
    public IReadOnlyDictionary<EngineTier, IReadOnlyList<EngineDescriptor>> ByTier { get; }
    public int TotalCount { get; }

    internal EngineRegistrySnapshot(IReadOnlyList<EngineDescriptor> descriptors)
    {
        TotalCount = descriptors.Count;

        ByTier = Enum.GetValues<EngineTier>()
            .ToDictionary(
                tier => tier,
                tier => (IReadOnlyList<EngineDescriptor>)descriptors
                    .Where(d => d.Tier == tier)
                    .OrderBy(d => d.EngineId)
                    .ToList()
                    .AsReadOnly()
            )
            .AsReadOnly();
    }

    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Engine Registry Snapshot — {TotalCount} engine(s) registered");
        sb.AppendLine(new string('=', 60));

        foreach (var tier in Enum.GetValues<EngineTier>())
        {
            var engines = ByTier[tier];
            sb.AppendLine();
            sb.AppendLine($"[{tier}] — {engines.Count} engine(s)");
            sb.AppendLine(new string('-', 40));

            if (engines.Count == 0)
            {
                sb.AppendLine("  (none)");
                continue;
            }

            foreach (var engine in engines)
            {
                sb.AppendLine($"  {engine.EngineId}");
                sb.AppendLine($"    Type:    {engine.EngineType.FullName}");
                sb.AppendLine($"    Command: {engine.CommandType.FullName}");
                sb.AppendLine($"    Result:  {engine.ResultType.FullName}");
            }
        }

        return sb.ToString();
    }
}
