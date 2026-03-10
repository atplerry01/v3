namespace Whycespace.System.Midstream.WhyceAtlas;

public sealed record AtlasInsight(
    string InsightId,
    string Category,
    string Description,
    decimal Confidence,
    DateTimeOffset GeneratedAt
);

public sealed class AtlasIntelligence
{
    private readonly List<AtlasInsight> _insights = new();

    public void RecordInsight(AtlasInsight insight) => _insights.Add(insight);

    public IReadOnlyList<AtlasInsight> GetInsights(string? category = null)
    {
        return category is null
            ? _insights
            : _insights.Where(i => i.Category == category).ToList();
    }
}
